using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof (Core.Logging.LogLevel))]
    public class when_getting_default_value_for_log_level
    {
        private static Core.Logging.LogLevel logLevel;
        private Because of = () => { logLevel = default(Core.Logging.LogLevel); };

        private It should_equals_default = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Default);
    }
}