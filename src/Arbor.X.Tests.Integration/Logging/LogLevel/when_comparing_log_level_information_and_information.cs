using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_comparing_log_level_information_and_information
    {
        private static Core.Logging.LogLevel logLevel;
        private Because of = () => { logLevel = Core.Logging.LogLevel.Information; };

        private It should_egual_information = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Information);
    }
}
