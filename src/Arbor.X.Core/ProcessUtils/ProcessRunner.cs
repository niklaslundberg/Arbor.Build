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

            Task<int> task = RunProcessAsync(executePath, formattedArguments, standardErrorAction, standardOutLog,
                                             cancellationToken, toolAction);

            var exitCode = await task;

            return new ExitCode(exitCode);
        }

        static Task<int> RunProcessAsync(string executePath, string formattedArguments,
                                         Action<string> standardErrorAction, Action<string> standardOutputLog,
                                         CancellationToken cancellationToken, Action<string> toolAction)
        {
            toolAction = toolAction ?? (message => { });
            var standardAction = standardOutputLog ?? (message => { });
            var errorAction = standardErrorAction ?? (message => { });

            var task = Task.Run(() =>
                {
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

                    using (var process = new Process
                                             {
                                                 StartInfo = processStartInfo
                                             })
                    {
                        if (redirectStandardError)
                        {
                            process.ErrorDataReceived += (sender, args) => errorAction(args.Data);
                        }
                        if (redirectStandardOutput)
                        {
                            process.OutputDataReceived += (sender, args) => standardAction(args.Data);
                        }
                        bool started = process.Start();

                        if (!started)
                        {
                            errorAction(string.Format("Process {0} could not be started", processWithArgs));
                            return Task.FromResult(-1);
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

                        toolAction(string.Format("The process '{0}' is running in {1}-bit mode", processWithArgs, bits));
                       
                        process.WaitForExit();

                        return Task.FromResult(process.ExitCode);
                    }
                }, cancellationToken);
            return task;
        }
    }
}