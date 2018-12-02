using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Processing;
using Machine.Specifications;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_process_longer_than_timeout
    {
        static string testPath;
        static ExitCode exitCode = new ExitCode(99);
        static TaskCanceledException exception;
        static ILogger logger = Logger.None;
        static string logFile;

        Cleanup after = () =>
        {
            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }

            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        };

        Establish context = () =>
        {
            testPath = Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}_Test_timeout.tmp.bat");

            logFile = @"C:\Temp\test.log";

            string batchContent = $@"@ECHO OFF
ECHO Waiting for 10 seconds
ping 127.0.0.1 -r 9
ECHO After batch file timeout
ECHO 123 > {logFile}
EXIT /b 2
";

            File.WriteAllText(testPath, batchContent, Encoding.Default);
        };

        Because of = () => RunAsync().Wait();

        It should_not_an_exit_code = () => exitCode.Code.ShouldEqual(99);

        It should_throw_a_task_canceled_exception = () => exception.ShouldNotBeNull();

        static async Task RunAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                exitCode =
                    await
                        Processing.ProcessRunner.ExecuteProcessAsync(testPath,
                            standardOutLog: (message, prefix) => logger.Information(message, "STANDARD"),
                            standardErrorAction: (message, prefix) => logger.Error(message, "ERROR"),
                            toolAction: (message, prefix) => logger.Information(message, "TOOL"),
                            verboseAction: (message, prefix) => logger.Information(message, "VERBOSE"),
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                exception = ex;
            }
        }
    }
}
