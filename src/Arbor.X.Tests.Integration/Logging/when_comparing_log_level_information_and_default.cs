using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_comparing_log_level_information_and_default
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = LogLevel.Information; };

        It should_equal_default = () => logLevel.ShouldEqual(LogLevel.Default);
    }
}