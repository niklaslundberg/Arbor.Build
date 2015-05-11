using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Core.ProcessUtils.ProcessRunner))]
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_process_longer_than_timeout
    {
        static string testPath;
        static ConsoleLogger logger;
        static ExitCode exitCode;
        static TaskCanceledException exception;

        Establish context = () =>
        {
            testPath = Path.Combine(Path.GetTempPath(), "Arbor.X.Test_timeout.tmp.bat");

            const string batchContent = @"@ECHO OFF
ECHO Waiting for 10 seconds
TIMEOUT /T 100
ECHO After batch file timeout
ECHO 123 > C:\Temp\test.log
EXIT /b 2
";

            File.WriteAllText(testPath, batchContent, Encoding.Default);
            logger = new ConsoleLogger("TEST ");
        };

        Because of = () => RunAsync().Wait();

        It should_not_an_exit_code = () => exitCode.ShouldBeNull();

        It should_throw_a_task_canceled_exception = () => exception.ShouldNotBeNull();

        static async Task RunAsync()
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                exitCode =
                    await
                        Core.ProcessUtils.ProcessRunner.ExecuteAsync(testPath,
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