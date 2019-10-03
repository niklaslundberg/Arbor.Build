using System;
using System.IO;
using Arbor.Build.Core.Bootstrapper;
using Arbor.Build.Core.IO;
using Arbor.Processing;
using Machine.Specifications;
using Serilog.Core;

namespace Arbor.Build.Tests.Integration.Bootstrapper
{
    [Ignore("Not complete")]
    [Subject(typeof(Core.Bootstrapper.AppBootstrapper))]
    public class when_running_bootstrapper
    {
        static Core.Bootstrapper.AppBootstrapper _appBootstrapper;

        static BootstrapStartOptions startOptions;
        static ExitCode exitCode;
        static DirectoryInfo baseDirectory;

        Cleanup after = () =>
        {
            try
            {
                baseDirectory.DeleteIfExists();
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex);
            }
        };

        Establish context = () =>
        {
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_Bootstrapper_Test_{Guid.NewGuid()}");

            baseDirectory = new DirectoryInfo(tempDirectoryPath).EnsureExists();
            Console.WriteLine("Temp directory is {0}", baseDirectory.FullName);

            startOptions = new BootstrapStartOptions(baseDirectory.FullName,
                true,
                "develop");
            _appBootstrapper = new Core.Bootstrapper.AppBootstrapper(Logger.None);
        };

        Because of = () => exitCode = _appBootstrapper.StartAsync(startOptions).Result;

        It should_return_success_exit_code = () => exitCode.IsSuccess.ShouldBeTrue();
    }
}
