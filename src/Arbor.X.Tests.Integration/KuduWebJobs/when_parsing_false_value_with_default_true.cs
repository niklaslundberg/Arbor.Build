using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebJobs
{
    [Subject(typeof(BoolExtensions))]
    public class when_parsing_false_value_with_default_true
    {
        static bool result;
        Because of = () => { result = "false".TryParseBool(defaultValue: true); };

        It should_be_false = () => result.ShouldBeFalse();
    }
}