using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arbor.Exceptions;
using Arbor.Processing.Core;

namespace Arbor.Processing
{
    public static class ProcessRunner
    {
        const string ToolName = "[" + nameof(ProcessRunner) + "] ";

        public static async Task<ExitCode> ExecuteAsync(string executePath,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<string> arguments = null,
            Action<string, string> standardOutLog = null,
            Action<string, string> standardErrorAction = null,
            Action<string, string> toolAction = null,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(executePath))
            {
                throw new ArgumentNullException(nameof(executePath));
            }

            if (!File.Exists(executePath))
            {
                throw new ArgumentException(
                    $"The executable file '{executePath}' does not exist",
                    nameof(executePath));
            }

            IEnumerable<string> usedArguments = arguments ?? Enumerable.Empty<string>();

            string formattedArguments = string.Join(" ", usedArguments.Select(arg => $"\"{arg}\""));

            Task<ExitCode> task = RunProcessAsync(executePath, formattedArguments, standardErrorAction, standardOutLog,
                cancellationToken, toolAction, verboseAction, environmentVariables, debugAction, addProcessNameAsLogCategory, addProcessRunnerCategory, parentPrefix);

            ExitCode exitCode = await task;

            return exitCode;
        }

        static async Task<ExitCode> RunProcessAsync(string executePath, string formattedArguments,
            Action<string, string> standardErrorAction, Action<string, string> standardOutputLog,
            CancellationToken cancellationToken, Action<string, string> toolAction,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null)
        {
            toolAction = toolAction ?? ((message, prefix) => { });
            Action<string, string> standardAction = standardOutputLog ?? ((message, prefix) => { });
            Action<string, string> errorAction = standardErrorAction ?? ((message, prefix) => { });
            Action<string, string> verbose = verboseAction ?? ((message, prefix) => { });
            Action<string, string> debug = debugAction ?? ((message, prefix) => { });

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            string processWithArgs = $"\"{executePath}\" {formattedArguments}".Trim();

            string toolCategory = parentPrefix + ToolName;
            toolAction($"Executing '{processWithArgs}'", toolCategory);

            bool useShellExecute = standardErrorAction == null && standardOutputLog == null;

            var category = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}] ";

            string outputCategory = parentPrefix + (addProcessRunnerCategory ? ToolName :"") + (addProcessNameAsLogCategory ? category : "");

            bool redirectStandardError = standardErrorAction != null;

            bool redirectStandardOutput = standardOutputLog != null;

            var processStartInfo = new ProcessStartInfo(executePath)
                                   {
                                       Arguments = formattedArguments,
                                       RedirectStandardError = redirectStandardError,
                                       RedirectStandardOutput = redirectStandardOutput,
                                       UseShellExecute = useShellExecute
                                   };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables.Add(environmentVariable.Key, environmentVariable.Value);
                }
            }

            var exitCode = new ExitCode(-1);

            var process = new Process
                          {
                              StartInfo = processStartInfo,
                              EnableRaisingEvents = true
                          };

            process.Disposed += (sender, args) =>
            {
                if (!taskCompletionSource.Task.IsCompleted)
                {
                    verbose($"Task was not completed, but process '{processWithArgs}' was disposed", toolCategory);
                    taskCompletionSource.TrySetResult(ExitCode.Failure);
                }
                verbose($"Disposed process '{processWithArgs}'", toolCategory);
            };

            if (redirectStandardError)
            {
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorAction(args.Data, outputCategory);
                    }
                };
            }
            if (redirectStandardOutput)
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        standardAction(args.Data, outputCategory);
                    }
                };
            }

            process.Exited += (sender, args) =>
            {
                var proc = (Process) sender;
                exitCode = new ExitCode(proc.ExitCode);
                toolAction($"Process '{processWithArgs}' exited with code {new ExitCode(proc.ExitCode)}", toolCategory);
                taskCompletionSource.SetResult(new ExitCode(proc.ExitCode));
            };

            int processId = -1;

            try
            {
                bool started = process.Start();

                if (!started)
                {
                    errorAction($"Process '{processWithArgs}' could not be started", toolCategory);
                    return ExitCode.Failure;
                }

                if (redirectStandardError)
                {
                    process.BeginErrorReadLine();
                }

                if (redirectStandardOutput)
                {
                    process.BeginOutputReadLine();
                }

                int bits = process.IsWin64() ? 64 : 32;

                try
                {
                    processId = process.Id;
                }
                catch (InvalidOperationException ex)
                {
                    debug($"Could not get process id for process '{processWithArgs}'. {ex}", toolCategory);
                }

                string temp = process.HasExited ? "was" : "is";

                verbose(
                    $"The process '{processWithArgs}' {temp} running in {bits}-bit mode",
                   toolCategory);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                errorAction($"An error occured while running process '{processWithArgs}': {ex}", toolCategory);
                taskCompletionSource.SetException(ex);
            }
            bool done = false;
            try
            {
                while (process.IsAlive(taskCompletionSource.Task, cancellationToken, done, processWithArgs, toolAction,
                    standardAction, errorAction, verbose))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    Task delay = Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

                    await delay;

                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        done = true;
                        exitCode = await taskCompletionSource.Task;
                    }
                    else if (taskCompletionSource.Task.IsCanceled)
                    {
                        exitCode = ExitCode.Failure;
                    }
                    else if (taskCompletionSource.Task.IsFaulted)
                    {
                        exitCode = ExitCode.Failure;
                    }
                }
            }
            finally
            {
                if (!exitCode.IsSuccess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (process != null && !process.HasExited)
                        {
                            try
                            {
                                toolAction($"Cancellation is requested, trying to kill process '{processWithArgs}'", toolCategory);

                                if (processId > 0)
                                {
                                    string args = $"/PID {processId}";
                                    string killProcessPath =
                                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                            "taskkill.exe");
                                    toolAction($"Running {killProcessPath} {args}", toolCategory);
                                    Process.Start(killProcessPath, args);

                                    errorAction(
                                        $"Killed process '{processWithArgs}' because cancellation was requested", toolCategory);
                                }
                                else
                                {
                                    debugAction(
                                        $"Could not kill process '{processWithArgs}', missing process id",
                                        toolCategory);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex.IsFatal())
                                {
                                    throw;
                                }

                                errorAction(
                                    $"ProcessRunner could not kill process '{processWithArgs}' when cancellation was requested", toolCategory);
                                errorAction(
                                    $"Could not kill process '{processWithArgs}' when cancellation was requested", toolCategory);
                                errorAction(ex.ToString(), toolCategory);
                            }
                        }
                    }
                }
                using (process)
                {
                    verbose(
                        $"Task status: {taskCompletionSource.Task.Status}, {taskCompletionSource.Task.IsCompleted}", toolCategory);
                    verbose($"Disposing process '{processWithArgs}'", toolCategory);
                }
            }

            verbose($"Process runner exit code {exitCode} for process '{processWithArgs}'", toolCategory);

            try
            {
                if (processId > 0)
                {
                    var stillRunningProcess = Process.GetProcesses().SingleOrDefault(p => p.Id == processId);

                    if (stillRunningProcess != null)
                    {
                        if (!stillRunningProcess.HasExited)
                        {
                            errorAction(
                                $"The process with ID {processId.ToString(CultureInfo.InvariantCulture)} '{processWithArgs}' is still running", toolCategory);

                            return ExitCode.Failure;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                debugAction($"Could not check processes. {ex}", toolCategory);
            }

            return exitCode;
        }

    }
}
