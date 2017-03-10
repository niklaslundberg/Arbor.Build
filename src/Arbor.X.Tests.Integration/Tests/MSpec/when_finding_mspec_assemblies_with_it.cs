using System;
using System.Collections.Generic;
using System.IO;
using Arbor.Processing.Core;
using Arbor.X.Core;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof (UnitTestFinder))]
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
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

            var tempPath = Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}_mspec_it_{DateTime.Now.ToString("yyyyMMddHHmmssfff_")}{Guid.NewGuid().ToString().Substring(0, 8)}");
            var root = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src");

            var combine = Path.Combine(root, "Arbor.X.Tests.Integration", "bin", "debug");


            tempDirectory = new DirectoryInfo(tempPath).EnsureExists();

            exitCode = DirectoryCopy.CopyAsync(combine, tempDirectory.FullName).Result;
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
        static ExitCode exitCode;
    }
}
