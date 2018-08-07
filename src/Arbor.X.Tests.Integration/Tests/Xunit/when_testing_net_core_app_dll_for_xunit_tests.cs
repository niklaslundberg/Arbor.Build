using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Arbor.X.Core.Tools.Testing;
using Arbor.X.Tests.Integration.Tests.MSpec;
using Machine.Specifications;
using Serilog.Core;
using Xunit;

namespace Arbor.X.Tests.Integration.Tests.Xunit
{
    [Ignore("local")]
    [Subject(typeof(UnitTestFinder))]
    public class when_testing_net_core_app_dll_for_xunit_tests
    {
        static UnitTestFinder finder;
        static HashSet<string> unitTestFixtureDlls;

        Establish context = () =>
        {
            var logger = Logger.None;
            finder = new UnitTestFinder(new List<Type>
                {
                    typeof(FactAttribute)
                },
                logger: logger);
        };

        Because of =
            () =>
            {
                string currentDirectory = Path.Combine(VcsTestPathHelper.FindVcsRootPath(),
                    "src",
                    "Arbor.X.Tests.NetCoreAppSamle");

                unitTestFixtureDlls = finder.GetUnitTestFixtureDlls(new DirectoryInfo(currentDirectory),
                    false,
                    new[] { "Arbor" }.ToImmutableArray(),
                    ".NETCoreApp");
            };

        It should_Behaviour = () => unitTestFixtureDlls.ShouldNotBeEmpty();
    }
}
