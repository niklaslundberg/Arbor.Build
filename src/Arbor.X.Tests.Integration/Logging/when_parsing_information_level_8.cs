using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_parsing_information_level_8
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = LogLevel.TryParse(8); };

        It should_equal_information_level = () => logLevel.Level.ShouldEqual(LogLevel.Information.Level);

        It should_equal_information = () => logLevel.ShouldEqual(LogLevel.Information);
    }
}