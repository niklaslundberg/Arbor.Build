using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.Tools.Testing;
using Machine.Specifications;
using Mono.Cecil;
using Serilog.Core;

namespace Arbor.Build.Tests.Integration.Tests.MSpec
{
    [Subject(typeof(UnitTestFinder))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_testing_this_test_type_for_subject
    {
        static UnitTestFinder finder;
        static bool isTestType;

        Establish context = () =>
        {
            var logger = Logger.None;
            finder = new UnitTestFinder(new List<Type>
                {
                    typeof(SubjectAttribute)
                },
                logger: logger);
        };

        Because of =
            () =>
            {
                Type typeToInvestigate = typeof(when_testing_this_test_type_for_subject);

                AssemblyDefinition assemblyDefinition =
                    AssemblyDefinition.ReadAssembly(typeToInvestigate.Assembly.Location);

                TypeDefinition typeDefinition =
                    assemblyDefinition.MainModule.Types.Single(t => t.FullName.Equals(typeToInvestigate.FullName));

                isTestType = finder.TryIsTypeTestFixture(typeDefinition);
            };

        It should_Behaviour = () => isTestType.ShouldBeTrue();
    }
}
