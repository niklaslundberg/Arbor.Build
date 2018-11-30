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
        private const string ToolName = "[" + nameof(ProcessRunner) + "] ";

        public static async Task<ExitCode> ExecuteAsync(
            string executePath,
            CancellationToken cancellationToken = default,
            IEnumerable<string> arguments = null,
            Action<string, string> standardOutLog = null,
            Action<string, string> standardErrorAction = null,
            Action<string, string> toolAction = null,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null,
            bool noWindow = true,
            bool shellExecute = false)
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

            Task<ExitCode> task = RunProcessAsync(executePath,
                formattedArguments,
                standardErrorAction,
                standardOutLog,
                cancellationToken,
                toolAction,
                verboseAction,
                environmentVariables,
                debugAction,
                addProcessNameAsLogCategory,
                addProcessRunnerCategory,
                parentPrefix,
                noWindow,
                shellExecute);

            ExitCode exitCode = await task.ConfigureAwait(false);

            return exitCode;
        }

        private static async Task<ExitCode> RunProcessAsync(
            string executePath,
            string formattedArguments,
            Action<string, string> standardErrorAction,
            Action<string, string> standardOutputLog,
            CancellationToken cancellationToken,
            Action<string, string> toolAction,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null,
            bool noWindow = true,
            bool shellExecute = false)
        {
            Action<string, string> standardAction = standardOutputLog;
            Action<string, string> errorAction = standardErrorAction;
            Action<string, string> verbose = verboseAction;
            Action<string, string> debug = debugAction;

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            string processWithArgs = $"\"{executePath}\" {formattedArguments}".Trim();

            var stopwatch = new Stopwatch();

            try
            {
                string toolCategory = parentPrefix + ToolName;
                toolAction?.Invoke($"Executing '{processWithArgs}'", toolCategory);

                string category = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}] ";

                string outputCategory = parentPrefix + (addProcessRunnerCategory ? ToolName : string.Empty) +
                                        (addProcessNameAsLogCategory ? category : string.Empty);

                bool redirectStandardError = standardErrorAction != null;

                bool redirectStandardOutput = standardOutputLog != null;

                var processStartInfo = new ProcessStartInfo(executePath)
                {
                    Arguments = formattedArguments,
                    RedirectStandardError = redirectStandardError,
                    RedirectStandardOutput = redirectStandardOutput,
                    UseShellExecute = shellExecute,
                    CreateNoWindow = noWindow
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
                        verbose?.Invoke($"Task was not completed, but process '{processWithArgs}' was disposed", toolCategory);
                        taskCompletionSource.TrySetResult(ExitCode.Failure);
                    }

                    verbose?.Invoke($"Disposed process '{processWithArgs}'", toolCategory);
                };

                if (redirectStandardError)
                {
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                        {
                            errorAction?.Invoke(args.Data, outputCategory);
                        }
                    };
                }

                if (redirectStandardOutput)
                {
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                        {
                            standardAction?.Invoke(args.Data, outputCategory);
                        }
                    };
                }

                process.Exited += (sender, args) =>
                {
                    var proc = (Process)sender;

                    try
                    {
                        exitCode = new ExitCode(proc.ExitCode);
                    }
                    catch (InvalidOperationException ex)
                    {
                        toolAction?.Invoke($"Could not get exit code for process, {ex}", toolCategory);

                        if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled &&
                            !taskCompletionSource.Task.IsFaulted)
                        {
                            taskCompletionSource.TrySetResult(new ExitCode(1));
                        }

                        return;
                    }

                    toolAction?.Invoke($"Process '{processWithArgs}' exited with code {exitCode}",
                        toolCategory);

                    taskCompletionSource.TrySetResult(new ExitCode(proc.ExitCode));
                };

                int processId = -1;

                try
                {
                    stopwatch.Start();
                    bool started = process.Start();

                    if (!started)
                    {
                        errorAction?.Invoke($"Process '{processWithArgs}' could not be started", toolCategory);
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

                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                    bool? isWin64 = process.IsWin64();

                    int? bits = isWin64.HasValue ? isWin64.Value ? 64 : 32 : default;

                    try
                    {
                        processId = process.Id;
                    }
                    catch (InvalidOperationException ex)
                    {
                        debug($"Could not get process id for process '{processWithArgs}'. {ex}", toolCategory);
                    }

                    string temp = process.HasExited ? "was" : "is";

                    verbose?.Invoke(
                        $"The process '{processWithArgs}' {temp} running in {bits?.ToString() ?? "N/A"}-bit mode",
                        toolCategory);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }

                    errorAction?.Invoke($"An error occured while running process '{processWithArgs}': {ex}", toolCategory);
                    taskCompletionSource.TrySetException(ex);
                }

                bool done = false;

                try
                {
                    while (process.IsAlive(taskCompletionSource.Task,
                        cancellationToken,
                        done,
                        processWithArgs,
                        toolAction,
                        standardAction,
                        errorAction,
                        verbose))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Task delay = Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

                        await delay.ConfigureAwait(false);

                        if (taskCompletionSource.Task.IsCompleted)
                        {
                            done = true;
                            exitCode = await taskCompletionSource.Task.ConfigureAwait(false);
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
                                    toolAction?.Invoke($"Cancellation is requested, trying to kill process '{processWithArgs}'",
                                        toolCategory);

                                    if (processId > 0)
                                    {
                                        string args = $"/PID {processId}";
                                        string killProcessPath =
                                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                                "taskkill.exe");
                                        toolAction?.Invoke($"Running {killProcessPath} {args}", toolCategory);

                                        using (Process killProcess = Process.Start(killProcessPath, args))
                                        {
                                        }

                                        errorAction?.Invoke(
                                            $"Killed process '{processWithArgs}' because cancellation was requested",
                                            toolCategory);
                                    }
                                    else
                                    {
                                        debugAction?.Invoke(
                                            $"Could not kill process '{processWithArgs}', missing process id",
                                            toolCategory);
                                    }
                                }
                                catch (Exception ex) when (!ex.IsFatal())
                                {
                                    errorAction?.Invoke(
                                        $"ProcessRunner could not kill process '{processWithArgs}' when cancellation was requested",
                                        toolCategory);
                                    errorAction?.Invoke(
                                        $"Could not kill process '{processWithArgs}' when cancellation was requested",
                                        toolCategory);
                                    errorAction?.Invoke(ex.ToString(), toolCategory);
                                }
                            }
                        }
                    }

                    using (process)
                    {
                        verbose?.Invoke(
                            $"Task status: {taskCompletionSource.Task.Status}, {taskCompletionSource.Task.IsCompleted}",
                            toolCategory);
                        verbose?.Invoke($"Disposing process '{processWithArgs}'", toolCategory);
                    }
                }

                verbose?.Invoke($"Process runner exit code {exitCode} for process '{processWithArgs}'", toolCategory);

                try
                {
                    if (processId > 0)
                    {
                        using (Process stillRunningProcess = Process.GetProcesses().SingleOrDefault(p => p.Id == processId))
                        {
                            if (stillRunningProcess != null)
                            {
                                if (!stillRunningProcess.HasExited)
                                {
                                    errorAction?.Invoke(
                                        $"The process with ID {processId.ToString(CultureInfo.InvariantCulture)} '{processWithArgs}' is still running",
                                        toolCategory);

                                    return ExitCode.Failure;
                                }
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

                    debugAction?.Invoke($"Could not check processes. {ex}", toolCategory);
                }

                return exitCode;
            }
            finally
            {
                stopwatch.Stop();

                Serilog.Log.Logger.Debug("Process {Process} took {DurationInMilliseconds} to run",
                    processWithArgs,
                    (int)stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
