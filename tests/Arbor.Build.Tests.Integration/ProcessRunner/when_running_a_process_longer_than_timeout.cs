using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.FS;
using Arbor.Processing;
using Machine.Specifications;
using Serilog;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_process_longer_than_timeout
    {
        static UPath testPath;
        static ExitCode exitCode = new ExitCode(99);
        static TaskCanceledException exception;
        static ILogger logger = Logger.None;
        static FileEntry logFile;

        Cleanup after = () =>
        {
            fs.DeleteFile(testPath);
            logFile.DeleteIfExists();

            fs.Dispose();
        };

        Establish context = () =>
        {
            fs = new PhysicalFileSystem();
            testPath = UPath.Combine(Path.GetTempPath().ParseAsPath(), $"{DefaultPaths.TempPathPrefix}_Test_timeout.tmp.bat");

            logFile = new FileEntry(fs, @"C:\Temp\test.log".ParseAsPath());

            string batchContent = $@"@ECHO OFF
ECHO Waiting for 10 seconds
ping 127.0.0.1 -r 9
ECHO After batch file timeout
ECHO 123 > {logFile}
EXIT /b 2
";
            using var stream = fs.OpenFile(testPath, FileMode.Create, FileAccess.Write);
            stream.WriteAllTextAsync(batchContent, Encoding.Default).Wait();
        };

        Because of = () => RunAsync().Wait();

        It should_not_an_exit_code = () => exitCode.Code.ShouldEqual(99);

        It should_throw_a_task_canceled_exception = () => exception.ShouldNotBeNull();
        static PhysicalFileSystem fs;

        static async Task RunAsync()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                exitCode =
                    await
                        Processing.ProcessRunner.ExecuteProcessAsync(fs.ConvertPathToInternal(testPath),
                            standardOutLog: (message, prefix) =>
                                logger.Information("[{Level}] {Message}", "STANDARD", message),
                            standardErrorAction: (message, prefix) =>
                                logger.Error("[{Level}] {Message}", "ERROR", message),
                            toolAction: (message, prefix) =>
                                logger.Information("[{Level}] {Message}", "TOOL", message),
                            verboseAction: (message, prefix) =>
                                logger.Information("[{Level}] {Message}", "VERBOSE", message),
                            cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                exception = ex;
            }
        }
    }
}
