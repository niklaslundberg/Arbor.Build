using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_verbose_string
    {
        private static Core.Logging.LogLevel logLevel;
        private Because of = () => { logLevel = Core.Logging.LogLevel.TryParse("verbose"); };

        private It should_equal_verbose_level = () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Verbose.Level);

        private It should_equal_verbose = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Verbose);
    }
}