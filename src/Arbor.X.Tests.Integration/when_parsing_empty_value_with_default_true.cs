using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration
{
    [Subject(typeof(BoolExtensions))]
    public class when_parsing_empty_value_with_default_true
    {
        static bool result;
        Because of = () => { result = "".TryParseBool(defaultValue: true); };

        It should_be_true = () => result.ShouldBeTrue();
    }
}