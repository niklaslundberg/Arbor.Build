using Arbor.Build.Core.GenericExtensions.Boolean;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.Boolean.BoolExtensions))]
    public class when_parsing_empty_value_with_default_true
    {
        static bool parsed;
        static bool result_value;

        Because of = () =>
        {
            parsed = string.Empty.TryParseBool(out bool result, true);
            result_value = result;
        };

        It parsed_should_be_false = () => parsed.ShouldBeFalse();

        It should_be_true = () => result_value.ShouldBeTrue();
    }
}
