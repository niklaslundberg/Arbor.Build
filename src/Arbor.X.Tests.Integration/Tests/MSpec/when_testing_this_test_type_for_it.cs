using System;
using System.Collections.Generic;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof (UnitTestFinder))]
    public class when_testing_this_test_type_for_it
    {
        static UnitTestFinder finder;
        static bool isTestType;

        Establish context = () =>
        {
            var logger = new ConsoleLogger {LogLevel = LogLevel.Verbose};
            finder = new UnitTestFinder(new List<Type>
                                        {
                                            typeof (It)
                                        }, logger: logger);
        };

        Because of =
            () => { isTestType = finder.TryIsTypeTestFixture(typeof(when_testing_this_test_type_for_it)); };

        It should_Behaviour = () => isTestType.ShouldBeTrue();
    }
}