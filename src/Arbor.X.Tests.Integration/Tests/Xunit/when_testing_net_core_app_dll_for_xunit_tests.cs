using System;
using System.Collections.Generic;
using System.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Arbor.X.Tests.Integration.Tests.MSpec;
using Machine.Specifications;
using Xunit;

namespace Arbor.X.Tests.Integration.Tests.Xunit
{
    [Ignore("local")]
    [Subject(typeof(UnitTestFinder))]
    public class when_testing_net_core_app_dll_for_xunit_tests
    {
        static UnitTestFinder finder;
        static bool isTestType;

        Establish context = () =>
        {
            var logger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            finder = new UnitTestFinder(new List<Type>
                {
                    typeof(FactAttribute)
                },
                logger: logger);
        };

        Because of =
            () =>
            {
                string currentDirectory = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src", "Arbor.X.Tests.NetCoreAppSamle");

                unitTestFixtureDlls = finder.GetUnitTestFixtureDlls(new DirectoryInfo(currentDirectory),
                    false,
                    "Arbor",
                    ".NETCoreApp");
            };

        It should_Behaviour = () => unitTestFixtureDlls.ShouldNotBeEmpty();
        static HashSet<string> unitTestFixtureDlls;
    }
}
