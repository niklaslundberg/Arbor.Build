using System;
using System.IO;
using Arbor.Processing.Core;
using Arbor.X.Bootstrapper;
using Arbor.X.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Bootstrapper
{
    [Ignore("Not complete")]
    [Subject(typeof (X.Bootstrapper.Bootstrapper))]
    public class when_running_bootstrapper
    {
        static X.Bootstrapper.Bootstrapper bootstrapper;

        static BootstrapStartOptions startOptions;
        static ExitCode exitCode;
        static DirectoryInfo baseDirectory;

        Cleanup after = () =>
        {
            try
            {
                baseDirectory.DeleteIfExists(recursive: true);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex);
            }
        };

        Establish context = () =>
        {
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}_Bootstrapper_Test_{Guid.NewGuid()}");

            baseDirectory = new DirectoryInfo(tempDirectoryPath).EnsureExists();
            Console.WriteLine("Temp directory is {0}", baseDirectory.FullName);


            startOptions = new BootstrapStartOptions(baseDirectory.FullName, prereleaseEnabled: true,
                branchName: "develop");
            bootstrapper = new X.Bootstrapper.Bootstrapper(LogLevel.Verbose);
        };

        Because of = () => { exitCode = bootstrapper.StartAsync(startOptions).Result; };

        It should_return_success_exit_code = () => exitCode.IsSuccess.ShouldBeTrue();
    }
}
