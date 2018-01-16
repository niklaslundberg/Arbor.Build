using System;
using System.Collections.Generic;
using System.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;
using Xunit;

namespace Arbor.X.Tests.Integration.Tests.Xunit
{
    [Machine.Specifications.Ignore("local only")]
    [Subject(typeof(UnitTestFinder))]
    public class when_testing_net_core_app_dll_for_xunit_tests2
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
                logger: logger, debugLogEnabled: true);
        };

        Because of =
            () =>
            {
                string currentDirectory = @"C:\Projects\Niklas\mohedascoutkar\src";

                unitTestFixtureDlls = finder.GetUnitTestFixtureDlls(new DirectoryInfo(currentDirectory),
                    false,
                    "Moheda",
                    ".NETCoreApp");
            };

        It should_Behaviour = () => unitTestFixtureDlls.ShouldNotBeEmpty();
        static HashSet<string> unitTestFixtureDlls;
    }
}
