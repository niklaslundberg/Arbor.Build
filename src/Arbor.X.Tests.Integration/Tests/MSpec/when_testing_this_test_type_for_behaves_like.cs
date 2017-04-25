using System;
using System.Collections.Generic;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof(UnitTestFinder))]
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_testing_this_test_type_for_behaves_like
    {
        private static UnitTestFinder finder;
        protected static Boolean Result;

        private Establish context = () =>
        {
            var logger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            finder = new UnitTestFinder(new List<Type>
                                        {
                                            typeof (Behaves_like<>)
                                        }, logger: logger);
        };

        private Because of =
            () => { Result = finder.TryIsTypeTestFixture(typeof(when_testing_this_test_type_for_behaves_like)); };

#pragma warning disable 169
        private Behaves_like<SampleBehaviors> sample_behaviors;
#pragma warning restore 169

    }
}