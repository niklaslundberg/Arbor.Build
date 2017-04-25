using System;
using System.IO;
using Arbor.Processing.Core;
using Arbor.X.Bootstrapper;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Bootstrapper
{
    [Ignore("Not complete")]
    [Subject(typeof(X.Bootstrapper.Bootstrapper))]
    public class when_running_bootstrapper
    {
        private static X.Bootstrapper.Bootstrapper bootstrapper;

        private static BootstrapStartOptions startOptions;
        private static ExitCode exitCode;
        private static DirectoryInfo baseDirectory;

        private Cleanup after = () =>
        {
            try
            {
                baseDirectory.DeleteIfExists(true);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex);
            }
        };

        private Establish context = () =>
        {
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_Bootstrapper_Test_{Guid.NewGuid()}");

            baseDirectory = new DirectoryInfo(tempDirectoryPath).EnsureExists();
            Console.WriteLine("Temp directory is {0}", baseDirectory.FullName);

            startOptions = new BootstrapStartOptions(baseDirectory.FullName,
                true,
                "develop");
            bootstrapper = new X.Bootstrapper.Bootstrapper(LogLevel.Verbose);
        };

        private Because of = () => { exitCode = bootstrapper.StartAsync(startOptions).Result; };

        private It should_return_success_exit_code = () => exitCode.IsSuccess.ShouldBeTrue();
    }
}
