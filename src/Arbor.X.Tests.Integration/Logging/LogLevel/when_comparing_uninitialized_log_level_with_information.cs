using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_comparing_uninitialized_log_level_with_information
    {
#pragma warning disable 649
        static Core.Logging.LogLevel logLevel;
#pragma warning restore 649

        It should_equal_information = () => logLevel.ShouldEqual(Core.Logging.LogLevel.Information);
    }
}
