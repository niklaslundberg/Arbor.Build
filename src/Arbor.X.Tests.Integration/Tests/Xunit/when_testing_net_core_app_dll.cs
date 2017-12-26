using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Arbor.X.Tests.Integration.Tests.MSpec;
using Machine.Specifications;
using Mono.Cecil;
using Xunit;

namespace Arbor.X.Tests.Integration.Tests.Xunit
{
    [Ignore("local")]
    [Subject(typeof(UnitTestFinder))]
    public class when_testing_net_core_app_dll
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
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src", "Arbor.X.Tests.NetCoreAppSamle", "Arbor.X.Tests.NetCoreAppSamle.dll"));

                TypeDefinition typeDefinition = assemblyDefinition.MainModule.Types.Single(t => t.FullName.StartsWith("Arbor", StringComparison.Ordinal));

                isTestType = finder.TryIsTypeTestFixture(typeDefinition);
            };

        It should_Behaviour = () => isTestType.ShouldBeTrue();
    }
}
