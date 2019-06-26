using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    public class when_running_process_without_logging
    {
        Because of = () => { RunAsync().Wait(TimeSpan.FromMilliseconds(5000)); };
        static ExitCode exitCode;


        private static async Task RunAsync()
        {
            exitCode =
                await
                    Processing.ProcessRunner.ExecuteProcessAsync(@"C:\Windows\System32\PING.EXE",
                        arguments: new[]{"127.0.0.1"},
                        standardOutLog: null,
                        standardErrorAction: null,
                        toolAction: null,
                        verboseAction: null,
                        debugAction:null,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        It should_return_exit_code_0 = () => { exitCode.ShouldEqual(ExitCode.Success); };
    }
}
