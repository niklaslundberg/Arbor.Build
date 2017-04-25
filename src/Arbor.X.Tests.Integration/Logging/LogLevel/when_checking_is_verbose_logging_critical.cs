using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_checking_is_verbose_logging_critical
    {
        private Because of = () => { isLogging = Core.Logging.LogLevel.Verbose.IsLogging(Core.Logging.LogLevel.Critical); };

        private It should_return_true = () => isLogging.ShouldBeTrue();

        private static bool isLogging;
    }
}