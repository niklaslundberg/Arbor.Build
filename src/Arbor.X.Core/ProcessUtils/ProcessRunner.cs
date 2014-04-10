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
            Action<string> standardOutLog = null,
            Action<string> standardErrorAction = null,
            Action<string> toolAction = null)
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
            Action<string> standardErrorAction, Action<string> standardOutputLog,
            CancellationToken cancellationToken, Action<string> toolAction, Action<string> verboseAction = null)
        {
            toolAction = toolAction ?? (message => { });
            var standardAction = standardOutputLog ?? (message => { });
            var errorAction = standardErrorAction ?? (message => { });
            var verbose = verboseAction ?? (message => { });

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            var processWithArgs = string.Format("\"{0}\" {1}", executePath, formattedArguments);

            toolAction(string.Format("[{0}] Executing: {1}", typeof (ProcessRunner).Name, processWithArgs));

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

            process.Disposed += (sender, args) => verbose(string.Format("Disposed process '{0}'", processWithArgs));

            if (redirectStandardError)
            {
                process.ErrorDataReceived += (sender, args) => errorAction(args.Data);
            }
            if (redirectStandardOutput)
            {
                process.OutputDataReceived += (sender, args) => standardAction(args.Data);
            }

            process.Exited += (sender, args) =>
            {
                var proc = (Process) sender;
                toolAction(string.Format("Process '{0}' exited with code {1}", processWithArgs, new ExitCode(proc.ExitCode)));
                taskCompletionSource.SetResult(new ExitCode(proc.ExitCode));
            };

            try
            {
                bool started = process.Start();

                if (!started)
                {
                    errorAction(string.Format("Process '{0}' could not be started", processWithArgs));
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

                verbose(string.Format("The process '{0}' {1} running in {2}-bit mode", processWithArgs, temp, bits));
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
            var exitCode = await taskCompletionSource.Task;

            using (process)
            {
            }

            return exitCode;
        }
    }
}