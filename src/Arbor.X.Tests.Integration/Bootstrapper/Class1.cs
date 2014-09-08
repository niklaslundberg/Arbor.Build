using System;
using System.IO;
using Arbor.X.Bootstrapper;
using Arbor.X.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;
using Machine.Specifications.Model;

namespace Arbor.X.Tests.Integration.Bootstrapper
{
    [Ignore("Not complete")]
    [Subject(typeof (Subject))]
    public class when_Specification
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
            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), "Arbor.X", "Bootstrapper", "Test",
                Guid.NewGuid().ToString());

            baseDirectory = new DirectoryInfo(tempDirectoryPath).EnsureExists();
            Console.WriteLine("Temp directory is " + baseDirectory.FullName);


            startOptions = new BootstrapStartOptions(baseDirectory.FullName, prereleaseEnabled: true, branchName: "develop");
            bootstrapper = new X.Bootstrapper.Bootstrapper(LogLevel.Verbose);
        };

        Because of = () => { exitCode = bootstrapper.StartAsync(startOptions).Result; };
        It should_Behaviour = () => exitCode.IsSuccess.ShouldBeTrue();
    }
}