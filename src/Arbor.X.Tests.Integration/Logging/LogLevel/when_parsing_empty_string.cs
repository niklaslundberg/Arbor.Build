using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_empty_string
    {
        static Core.Logging.LogLevel logLevel;
        Because of = () => { logLevel = Core.Logging.LogLevel.TryParse(""); };

        It should_equal_default = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Default);

        It should_equal_default_level = () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Default.Level);
    }
}
