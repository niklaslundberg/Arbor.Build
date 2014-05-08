using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_comparing_log_level_information_and_information
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = LogLevel.Information; };

        It should_egual_information = () => logLevel.ShouldEqual(LogLevel.Information);
    }
}