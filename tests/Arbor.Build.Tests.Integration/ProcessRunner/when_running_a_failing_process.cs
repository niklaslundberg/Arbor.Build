using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Arbor.Processing;
using Machine.Specifications;
using Serilog;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.ProcessRunner;

[Subject(typeof(Processing.ProcessRunner))]
[Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
public class when_running_a_failing_process
{
    static UPath testPath;
    static ILogger logger = Logger.None;
    static ExitCode exitCode;

    Cleanup after = () =>
    {
        fs.DeleteFile(testPath);

        fs.Dispose();
    };

    Establish context = () =>
    {
        fs = new PhysicalFileSystem();
        testPath = UPath.Combine(Path.GetTempPath().ParseAsPath(), $"{DefaultPaths.TempPathPrefix}Test_fail.tmp.bat");
        const string batchContent = @"@ECHO OFF
EXIT /b 3
";

        using var stream = fs.OpenFile(testPath, FileMode.Create, FileAccess.Write);

        stream.WriteAllTextAsync(batchContent).Wait();
    };

    Because of = () => RunAsync().Wait();

    It should_return_exit_code_from_process = () => exitCode.Code.ShouldEqual(3);
    static IFileSystem fs;

    static async Task RunAsync()
    {
        try
        {
            exitCode =
                await
                    Processing.ProcessRunner.ExecuteProcessAsync(fs.ConvertPathToInternal(testPath),
                            standardOutLog: (message, prefix) => logger.Information("{Message}", message),
                            standardErrorAction: (message, prefix) => logger.Error("{Message}", message),
                            toolAction: (message, prefix) => logger.Information("{Message}", message),
                            verboseAction: (message, prefix) => logger.Information("{Message}", message))
                        .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}