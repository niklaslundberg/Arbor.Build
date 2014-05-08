using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_comparing_uninitialized_log_level_with_information
    {
        static LogLevel logLevel;
        Because of = () => { };

        It should_equal_information = () => logLevel.ShouldEqual(LogLevel.Information);
    }
}