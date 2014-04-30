using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.X.Core.ProcessUtils
{
    public static class ProcessRunner
    {
        public static async Task<ExitCode> ExecuteAsync(string executePath,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<string> arguments = null,
            Action<string, string> standardOutLog = null,
            Action<string, string> standardErrorAction = null,
            Action<string, string> toolAction = null)
        {
            if (string.IsNullOrWhiteSpace(executePath))
            {
                throw new ArgumentNullException("executePath");
            }

            if (!File.Exists(executePath))
            {
                throw new ArgumentException(string.Format("The executable file '{0}' does not exist", executePath),
                    "executePath");
            }

            var usedArguments = arguments ?? Enumerable.Empty<string>();

            string formattedArguments = string.Join(" ", usedArguments.Select(arg => string.Format("\"{0}\"", arg)));

            Task<ExitCode> task = RunProcessAsync(executePath, formattedArguments, standardErrorAction, standardOutLog,
                cancellationToken, toolAction);

            var exitCode = await task;

            return exitCode;
        }

        static async Task<ExitCode> RunProcessAsync(string executePath, string formattedArguments,
            Action<string, string> standardErrorAction, Action<string, string> standardOutputLog,
            CancellationToken cancellationToken, Action<string, string> toolAction, Action<string, string> verboseAction = null)
        {
            toolAction = toolAction ?? ((message, prefix) => { });
            var standardAction = standardOutputLog ?? ((message, prefix) => { });
            var errorAction = standardErrorAction ?? ((message, prefix) => { });
            var verbose = verboseAction ?? ((message, prefix) => { });

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            var processWithArgs = string.Format("\"{0}\" {1}", executePath, formattedArguments);

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

            var process = new Process
                          {
                              StartInfo = processStartInfo,
                              EnableRaisingEvents = true
                          };

            process.Disposed += (sender, args) =>
            {
                if (!taskCompletionSource.Task.IsCompleted || !taskCompletionSource.Task.IsFaulted ||
                    !taskCompletionSource.Task.IsCanceled)
                {
                    toolAction("Task was not completed, but is disposed",null);
                    taskCompletionSource.TrySetResult(ExitCode.Failure);
                }
                verbose(string.Format("Disposed process '{0}'", processWithArgs), null);
            };

            if (redirectStandardError)
            {
                process.ErrorDataReceived += (sender, args) => errorAction(args.Data, null);
            }
            if (redirectStandardOutput)
            {
                process.OutputDataReceived += (sender, args) => standardAction(args.Data, null);
            }

            process.Exited += (sender, args) =>
            {
                var proc = (Process) sender;
                toolAction(string.Format("Process '{0}' exited with code {1}", processWithArgs,
                    new ExitCode(proc.ExitCode)), null);
                taskCompletionSource.SetResult(new ExitCode(proc.ExitCode));
            };

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

                var bits = process.IsWin64() ? 64 : 32;

                var temp = process.HasExited ? "was" : "is";

                verbose(string.Format("The process '{0}' {1} running in {2}-bit mode", processWithArgs, temp, bits), null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
            bool done = false;

            var exitCode = new ExitCode(-1);

            while (IsAlive(process, taskCompletionSource.Task, cancellationToken, done))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        process.Kill();
                        errorAction(string.Format("Killed process {0} because cancellation was requested", processWithArgs), null);
                        return ExitCode.Failure;
                    }
                    catch (Exception ex)
                    {
                        errorAction(string.Format("Could not kill process {0} when cancellation was requested", processWithArgs), null);
                        errorAction(ex.ToString(), null);
                        return ExitCode.Failure;
                    }
                }

                var delay = Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
                
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

            using (process)
            {
            }

            return exitCode;
        }

        static bool IsAlive(Process process, Task<ExitCode> task, CancellationToken cancellationToken, bool done)
        {
            if (process == null)
            {
                return false;
            }

            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (done)
            {
                return false;
            }

            return true;
        }
    }
}