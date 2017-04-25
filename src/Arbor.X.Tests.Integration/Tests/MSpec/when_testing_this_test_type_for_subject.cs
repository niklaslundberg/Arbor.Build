using System;
using System.Collections.Generic;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof(UnitTestFinder))]
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_testing_this_test_type_for_subject
    {
        private static UnitTestFinder finder;
        private static bool isTestType;

        private Establish context = () =>
        {
            var logger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            finder = new UnitTestFinder(new List<Type>
                                        {
                                            typeof (SubjectAttribute)
                                        }, logger: logger);
        };

        private Because of =
            () => { isTestType = finder.TryIsTypeTestFixture(typeof(when_testing_this_test_type_for_subject)); };

        private It should_Behaviour = () => isTestType.ShouldBeTrue();
    }
}