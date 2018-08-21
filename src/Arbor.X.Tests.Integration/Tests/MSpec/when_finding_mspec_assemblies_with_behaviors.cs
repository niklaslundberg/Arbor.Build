using System;
using System.Collections.Generic;
using System.IO;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Processing.Core;
using Machine.Specifications;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Tests.Integration.Tests.MSpec
{
    [Ignore("Recursive")]
    [Subject(typeof(UnitTestFinder))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_finding_mspec_assemblies_with_behaviors
    {
        static UnitTestFinder finder;
        static IReadOnlyCollection<string> dlls;

        static DirectoryInfo tempDirectory;
        static ExitCode exitCode;

        Establish context = () =>
        {
            ILogger logger = Logger.None;
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

        Because of = () => dlls = finder.GetUnitTestFixtureDlls(tempDirectory);

        It should_Behaviour = () =>
        {
            foreach (string dll in dlls)
            {
                Console.WriteLine(dll);
            }
        };
    }
}
