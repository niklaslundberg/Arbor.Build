using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.ProcessRunner
{
    [Subject(typeof(Processing.ProcessRunner))]
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_a_failing_process
    {
        static string testPath;
        static ConsoleLogger logger;
        static ExitCode exitCode;

        Cleanup after = () =>
        {
            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }
        };

        Establish context = () =>
        {
            testPath = Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}Test_fail.tmp.bat");
            const string batchContent = @"@ECHO OFF
EXIT /b 3
";
            File.WriteAllText(testPath, batchContent, Encoding.Default);
            logger = new ConsoleLogger("TEST ");
        };

        Because of = () => RunAsync().Wait();

        It should_return_exit_code_from_process = () => exitCode.Result.ShouldEqual(3);

        static async Task RunAsync()
        {
            try
            {
                exitCode =
                    await
                        Processing.ProcessRunner.ExecuteAsync(testPath,
                            standardOutLog: (message, prefix) => logger.Write(message, "STANDARD"),
                            standardErrorAction: (message, prefix) => logger.WriteError(message, "ERROR"),
                            toolAction: (message, prefix) => logger.Write(message, "TOOL"),
                            verboseAction: (message, prefix) => logger.Write(message, "VERBOSE"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
