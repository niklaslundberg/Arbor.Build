using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_information_level_8
    {
        static Core.Logging.LogLevel logLevel;
        Because of = () => { logLevel = Core.Logging.LogLevel.TryParse(8); };

        It should_equal_information = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Information);

        It should_equal_information_level =
            () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Information.Level);
    }
}
