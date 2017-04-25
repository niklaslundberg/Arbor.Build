using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_empty_string
    {
        private static Core.Logging.LogLevel logLevel;
        private Because of = () => { logLevel = Core.Logging.LogLevel.TryParse(""); };

        private It should_equal_default_level = () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Default.Level);

        private It should_equal_default = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Default);
    }
}
