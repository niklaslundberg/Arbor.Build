using System;
using System.Collections.Generic;
using System.Linq;

using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;
using Mono.Cecil;
using Serilog.Core;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Ignore("Self")]
    [Subject(typeof(UnitTestFinder))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_testing_this_test_type_for_behaves_like
    {
        static UnitTestFinder finder;
        protected static bool Result;

        Establish context = () =>
        {

            var logger = Logger.None;
            finder = new UnitTestFinder(new List<Type>
                {
                    typeof(Behaves_like<>)
                },
                logger: logger);
        };

        Because of =
            () =>
            {
                Type typeToInvestigate = typeof(when_testing_this_test_type_for_behaves_like);

                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(typeToInvestigate.Assembly.Location);

                TypeDefinition typeDefinition = assemblyDefinition.MainModule.Types.Single(t => t.FullName.Equals(typeToInvestigate.FullName));

                Result = finder.TryIsTypeTestFixture(typeDefinition);
            };

#pragma warning disable 169
        Behaves_like<SampleBehaviors> sample_behaviors;
#pragma warning restore 169
    }
}
