using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_parsing_information_string_uppercase
    {
        static Core.Logging.LogLevel logLevel;
        Because of = () => { logLevel = Core.Logging.LogLevel.TryParse("INFORMATION"); };

        It should_equal_information_level = () => logLevel.Level.ShouldEqual(Core.Logging.LogLevel.Information.Level);

        It should_equal_information = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Information);
    }
}