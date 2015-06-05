using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.Exceptions;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.ProcessUtils
{
    public static class ProcessRunner
    {
        public static Task<ExitCode> ExecuteAsync(string executePath,
            IEnumerable<string> arguments = null, ILogger logger = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {

            var usedLogger = logger ?? new NullLogger();


            return ExecuteAsync(executePath, cancellationToken, arguments, standardOutLog: usedLogger.Write,
                standardErrorAction: usedLogger.WriteError,
                verboseAction: usedLogger.WriteVerbose, toolAction: usedLogger.Write,
                environmentVariables: environmentVariables,
                debugAction: usedLogger.WriteDebug);
        }

        public static async Task<ExitCode> ExecuteAsync(string executePath,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<string> arguments = null,
            Action<string, string> standardOutLog = null,
            Action<string, string> standardErrorAction = null,
            Action<string, string> toolAction = null,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null)
        {
            if (string.IsNullOrWhiteSpace(executePath))
            {
                throw new ArgumentNullException(nameof(executePath));
            }

            if (!File.Exists(executePath))
            {
                throw new ArgumentException(string.Format("The executable file '{0}' does not exist", executePath),
                    nameof(executePath));
            }

            IEnumerable<string> usedArguments = arguments ?? Enumerable.Empty<string>();

            string formattedArguments = string.Join(" ", usedArguments.Select(arg => string.Format("\"{0}\"", arg)));

            Task<ExitCode> task = RunProcessAsync(executePath, formattedArguments, standardErrorAction, standardOutLog,
                cancellationToken, toolAction, verboseAction, environmentVariables, debugAction);

            ExitCode exitCode = await task;

            return exitCode;
        }

        static async Task<ExitCode> RunProcessAsync(string executePath, string formattedArguments,
            Action<string, string> standardErrorAction, Action<string, string> standardOutputLog,
            CancellationToken cancellationToken, Action<string, string> toolAction,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null)
        {
            toolAction = toolAction ?? ((message, prefix) => { });
            Action<string, string> standardAction = standardOutputLog ?? ((message, prefix) => { });
            Action<string, string> errorAction = standardErrorAction ?? ((message, prefix) => { });
            Action<string, string> verbose = verboseAction ?? ((message, prefix) => { });
            Action<string, string> debug = debugAction ?? ((message, prefix) => { });

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            string processWithArgs = string.Format("\"{0}\" {1}", executePath, formattedArguments).Trim();

            toolAction(string.Format("[{0}] Executing: {1}", typeof (ProcessRunner).Name, processWithArgs), null);

            bool useShellExecute = standardErrorAction == null && standardOutputLog == null;

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
                    verbose("Task was not completed, but process was disposed", null);
                    taskCompletionSource.TrySetResult(ExitCode.Failure);
                }
                verbose(string.Format("Disposed process '{0}'", processWithArgs), null);
            };

            if (redirectStandardError)
            {
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorAction(args.Data, null);
                    }
                };
            }
            if (redirectStandardOutput)
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        standardAction(args.Data, null);
                    }
                };
            }

            process.Exited += (sender, args) =>
            {
                var proc = (Process) sender;
                toolAction(string.Format("Process '{0}' exited with code {1}", processWithArgs,
                    new ExitCode(proc.ExitCode)), null);
                taskCompletionSource.SetResult(new ExitCode(proc.ExitCode));
            };

            int processId = -1;

            try
            {
                bool started = process.Start();

                if (!started)
                {
                    errorAction(string.Format("Process '{0}' could not be started", processWithArgs), null);
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
                    debug(string.Format("Could not get process id for process '{0}'. {1}", processWithArgs, ex),null);
                }

                string temp = process.HasExited ? "was" : "is";

                verbose(string.Format("The process '{0}' {1} running in {2}-bit mode", processWithArgs, temp, bits),
                    null);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                errorAction(string.Format("An error occured while running process {0}: {1}", processWithArgs, ex), null);
                taskCompletionSource.SetException(ex);
            }
            bool done = false;
            try
            {
                while (IsAlive(process, taskCompletionSource.Task, cancellationToken, done, processWithArgs, toolAction,
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
                                toolAction(
                                    string.Format("Cancellation is requested, trying to kill process {0}",
                                        processWithArgs), null);

                                if (processId > 0)
                                {
                                    string args = string.Format("/PID {0}", processId);
                                    string killProcessPath =
                                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                            "taskkill.exe");
                                    toolAction(string.Format("Running {0} {1}", killProcessPath, args), null);
                                    Process.Start(killProcessPath, args);

                                    errorAction(
                                        string.Format("Killed process {0} because cancellation was requested",
                                            processWithArgs), null);
                                }
                                else
                                {
                                    debugAction(string.Format("Could not kill process '{0}', missing process id", processWithArgs),
                                        null);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex.IsFatal())
                                {
                                    throw;
                                }

                                errorAction(
                                    string.Format(
                                        "ProcessRunner could not kill process {0} when cancellation was requested",
                                        processWithArgs), null);
                                errorAction(
                                    string.Format("Could not kill process {0} when cancellation was requested",
                                        processWithArgs), null);
                                errorAction(ex.ToString(), null);
                            }
                        }
                    }
                }
                using (process)
                {
                    verbose(string.Format("Task status: {0}, {1}", taskCompletionSource.Task.Status, taskCompletionSource.Task.IsCompleted), null);
                    verbose(string.Format("Disposing process {0}", processWithArgs), null);
                }
            }

            verbose(string.Format("Process runner exit code {0} for process {1}", exitCode, processWithArgs), null);

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
                                string.Format("The process with ID {0} '{1}' is still running", processId.ToString(CultureInfo.InvariantCulture), processWithArgs), null);

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
                debugAction(string.Format("Could not check processes. {0}",  ex), null);
            }

            return exitCode;
        }

        static bool IsAlive(Process process, Task<ExitCode> task, CancellationToken cancellationToken, bool done,
            string processWithArgs, Action<string, string> toolAction, Action<string, string> standardAction,
            Action<string, string> errorAction, Action<string, string> verbose)
        {
            if (process == null)
            {
                verbose(string.Format("Process {0} does no longer exist", processWithArgs), null);
                return false;
            }

            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                TaskStatus status = task.Status;
                verbose(string.Format("Task status for process {0} is {1}", processWithArgs, status), null);
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                verbose(string.Format("Cancellation is requested for process {0}", processWithArgs), null);
                return false;
            }

            if (done)
            {
                verbose(string.Format("Process {0} is flagged as done", processWithArgs), null);
                return false;
            }

            return true;
        }
    }
}