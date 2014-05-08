using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof(LogLevel))]
    public class when_parsing_level_int_min
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = LogLevel.TryParse(int.MinValue); };

        It should_equal_default_level = () => logLevel.Level.ShouldEqual(LogLevel.Default.Level);

        It should_equal_default = () => logLevel.ShouldEqual(LogLevel.Default);
    }
}