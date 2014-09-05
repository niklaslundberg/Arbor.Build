using System;
using System.Collections.Generic;
using System.IO;
using Arbor.Aesculus.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof (UnitTestFinder))]
    [Tags("Arbor_X_Recursive")]
    public class when_finding_mspec_assemblies_with_it
    {
        static UnitTestFinder finder;
        static IReadOnlyCollection<string> dlls;

        Establish context = () =>
        {
            var logger = new ConsoleLogger {LogLevel = LogLevel.Verbose};
            finder = new UnitTestFinder(new List<Type>
                                        {
                                            typeof (It)
                                        }, logger: logger);

            var tempPath = Path.Combine(Path.GetTempPath(), "Arbor.X", "MSpec", Guid.NewGuid().ToString());
            var root = Path.Combine(VcsPathHelper.FindVcsRootPath(), "src");

            var combine = Path.Combine(root, "Arbor.X.Tests.Integration", "bin", "debug");

        
            tempDirectory = new DirectoryInfo(tempPath).EnsureExists();

            DirectoryCopy.Copy(combine, tempDirectory.FullName);
        };

        Because of = () => { dlls = finder.GetUnitTestFixtureDlls(tempDirectory); };

        It should_Behaviour = () =>
        {
            foreach (string dll in dlls)
            {
                Console.WriteLine(dll);
            }
        };

        static DirectoryInfo tempDirectory;
    }
}