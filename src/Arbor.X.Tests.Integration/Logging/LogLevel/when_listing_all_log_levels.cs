using System.Collections.Generic;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging.LogLevel
{
    [Subject(typeof(Core.Logging.LogLevel))]
    public class when_listing_all_log_levels
    {
        private static IEnumerable<Core.Logging.LogLevel> allValues;
        private Because of = () => { allValues = Core.Logging.LogLevel.AllValues; };

        private It should_contains_critical_error_warning_information_verbose = () => allValues.ShouldContain(new[]
        {
            Core.Logging.LogLevel
                .Critical,
            Core.Logging.LogLevel.Error,
            Core.Logging.LogLevel
                .Warning,
            Core.Logging.LogLevel
                .Information,
            Core.Logging.LogLevel
                .Verbose
        });
    }
}
