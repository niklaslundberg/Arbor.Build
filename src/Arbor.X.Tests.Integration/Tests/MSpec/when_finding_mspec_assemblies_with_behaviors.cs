using System;
using System.Collections.Generic;
using System.IO;
using Arbor.Processing.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof(UnitTestFinder))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_finding_mspec_assemblies_with_behaviors
    {
        private static UnitTestFinder finder;
        private static IReadOnlyCollection<string> dlls;

        private static DirectoryInfo tempDirectory;
        private static ExitCode exitCode;

        private Establish context = () =>
        {
            var logger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            finder = new UnitTestFinder(new List<Type>
                {
                    typeof(BehaviorsAttribute)
                },
                logger: logger);

            string root = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src");

            string combine = Path.Combine(root, "Arbor.X.Tests.Integration", "bin", "debug");

            string tempPath = Path.Combine(Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_mspec_beh_{DateTime.Now.ToString("yyyyMMddHHmmssfff_")}{Guid.NewGuid().ToString().Substring(0, 8)}");

            tempDirectory = new DirectoryInfo(tempPath).EnsureExists();

            exitCode = DirectoryCopy.CopyAsync(combine, tempDirectory.FullName).Result;
        };

        private Because of = () => { dlls = finder.GetUnitTestFixtureDlls(tempDirectory); };

        private It should_Behaviour = () =>
        {
            foreach (string dll in dlls)
            {
                Console.WriteLine(dll);
            }
        };
    }
}
