using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_information_string_mixed_case
    {
        private static Core.Logging.LogLevel logLevel;
        private Because of = () => { logLevel = Core.Logging.LogLevel.TryParse("Information"); };

        private It should_equal_information_level =
            () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Information.Level);

        private It should_equal_information = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Information);
    }
}
