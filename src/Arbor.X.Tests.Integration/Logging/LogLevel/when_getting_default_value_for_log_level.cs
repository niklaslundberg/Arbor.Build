using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof (Core.Logging.LogLevel))]
    public class when_getting_default_value_for_log_level
    {
        static Core.Logging.LogLevel logLevel;
        Because of = () => { logLevel = default(Core.Logging.LogLevel); };

        It should_equals_default = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Default);
    }
}