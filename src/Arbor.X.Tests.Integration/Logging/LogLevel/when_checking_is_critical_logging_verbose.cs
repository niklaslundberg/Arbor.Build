using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_checking_is_critical_logging_verbose
    {
        private static bool isLogging;

        private Because of = () =>
        {
            isLogging = Core.Logging.LogLevel.Critical.IsLogging(Core.Logging.LogLevel.Verbose);
        };

        private It should_return_false = () => isLogging.ShouldBeFalse();
    }
}
