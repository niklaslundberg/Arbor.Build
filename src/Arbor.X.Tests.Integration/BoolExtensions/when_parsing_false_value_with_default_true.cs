using Arbor.Build.Core.GenericExtensions.Bools;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.Bools.BoolParseExtensions))]
    public class when_parsing_false_value_with_default_true
    {
        static bool parsed;

        static bool parsed_result;

        Because of = () =>
        {
            parsed = "false".TryParseBool(out bool result, true);
            parsed_result = result;
        };

        It parsed_should_be_true = () => parsed.ShouldBeTrue();

        It should_be_false = () => parsed_result.ShouldBeFalse();
    }
}
