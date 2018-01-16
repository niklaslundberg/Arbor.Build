using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_verbose_level_16
    {
        static Core.Logging.LogLevel logLevel;
        Because of = () => { logLevel = Core.Logging.LogLevel.TryParse(16); };

        It should_equal_verbose = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Verbose);

        It should_equal_verbose_level = () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Verbose.Level);
    }
}
