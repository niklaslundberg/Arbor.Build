using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.ProcessRunner;

[Subject(typeof(Processing.ProcessRunner))]
public class when_running_process_without_logging
{
    static ExitCode exitCode;

    Because of = () => { RunAsync().Wait(TimeSpan.FromMilliseconds(5000)); };

    It should_return_exit_code_0 = () => { exitCode.ShouldEqual(ExitCode.Success); };

    static async Task RunAsync() => exitCode =
        await
            Processing.ProcessRunner.ExecuteProcessAsync(@"C:\Windows\System32\PING.EXE",
                new[] { "127.0.0.1" },
                (s, s1) => { },
                null,
                null,
                null,
                debugAction: null,
                noWindow: true,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
}