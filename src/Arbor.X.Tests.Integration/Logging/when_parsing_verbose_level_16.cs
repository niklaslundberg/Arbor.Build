using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof(LogLevel))]
    public class when_parsing_verbose_level_16
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = LogLevel.TryParse(16); };

        It should_equal_verbose_level = () => logLevel.Level.ShouldEqual(LogLevel.Verbose.Level);

        It should_equal_verbose = () => logLevel.ShouldEqual(LogLevel.Verbose);
    }
}