using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Logging
{
    [Subject(typeof (LogLevel))]
    public class when_getting_default_value_for_log_level
    {
        static LogLevel logLevel;
        Because of = () => { logLevel = default(LogLevel); };

        It should_equals_default = () => logLevel.ShouldEqual(LogLevel.Default);
    }
}