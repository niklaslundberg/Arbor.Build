using System; using Serilog;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.IO;

using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;
using Serilog.Core;

namespace Arbor.X.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_process_longer_than_timeout
    {
        static string testPath;
        static ExitCode exitCode = new ExitCode(99);
        static TaskCanceledException exception;
        static ILogger logger = Logger.None;
        Cleanup after = () =>
        {
            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }
        };

        Establish context = () =>
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

        };

        Because of = () => RunAsync().Wait();

        It should_not_an_exit_code = () => exitCode.Result.ShouldEqual(99);

        It should_throw_a_task_canceled_exception = () => exception.ShouldNotBeNull();

        static async Task RunAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                exitCode =
                    await
                        Processing.ProcessRunner.ExecuteAsync(testPath,
                            standardOutLog: (message, prefix) => logger.Information(message, "STANDARD"),
                            standardErrorAction: (message, prefix) => logger.Error(message, "ERROR"),
                            toolAction: (message, prefix) => logger.Information(message, "TOOL"),
                            verboseAction: (message, prefix) => logger.Information(message, "VERBOSE"),
                            cancellationToken: cancellationTokenSource.Token);
            }
            catch (TaskCanceledException ex)
            {
                exception = ex;
            }
        }
    }
}
