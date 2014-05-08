using System.Collections.Generic;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_listing_all_log_levels
    {
        static IEnumerable<LogLevel> allValues;
        Because of = () => { allValues = LogLevel.AllValues; };

        It should_contains_critical_error_warning_information_verbose = () => allValues.ShouldContain(new[]
                                                                                                      {
                                                                                                          LogLevel
                                                                                                              .Critical,
                                                                                                          LogLevel.Error,
                                                                                                          LogLevel
                                                                                                              .Warning,
                                                                                                          LogLevel
                                                                                                              .Information,
                                                                                                          LogLevel
                                                                                                              .Verbose,
                                                                                                      });
    }
}