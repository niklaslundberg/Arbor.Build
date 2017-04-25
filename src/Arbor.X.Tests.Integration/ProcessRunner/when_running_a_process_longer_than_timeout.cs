using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_process_longer_than_timeout
    {
        private static string testPath;
        private static ConsoleLogger logger;
        private static ExitCode exitCode = new ExitCode(99);
        private static TaskCanceledException exception;

        private Establish context = () =>
        {
            testPath = Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}_Test_timeout.tmp.bat");

            const string batchContent = @"@ECHO OFF
ECHO Waiting for 10 seconds
ping 127.0.0.1 -r 9
ECHO After batch file timeout
ECHO 123 > C:\Temp\test.log
EXIT /b 2
";

            File.WriteAllText(testPath, batchContent, Encoding.Default);
            logger = new ConsoleLogger("TEST ");
        };

        private Because of = () => RunAsync().Wait();

        private It should_not_an_exit_code = () => exitCode.Result.ShouldEqual(99);

        private It should_throw_a_task_canceled_exception = () => exception.ShouldNotBeNull();

        private static async Task RunAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                exitCode =
                    await
                        Processing.ProcessRunner.ExecuteAsync(testPath,
                            standardOutLog: (message, prefix) => logger.Write(message, "STANDARD"),
                            standardErrorAction: (message, prefix) => logger.WriteError(message, "ERROR"),
                            toolAction: (message, prefix) => logger.Write(message, "TOOL"),
                            verboseAction: (message, prefix) => logger.Write(message, "VERBOSE"),
                            cancellationToken: cancellationTokenSource.Token);
            }
            catch (TaskCanceledException ex)
            {
                exception = ex;
            }
        }
    }
}
