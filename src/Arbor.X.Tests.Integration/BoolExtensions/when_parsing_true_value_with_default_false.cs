using Arbor.Build.Core.GenericExtensions.Boolean;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.Boolean.BoolParseExtensions))]
    public class when_parsing_true_value_with_default_false
    {
        static bool parsed;
        Because of = () => parsed = "true".TryParseBool(out bool result);

        It should_be_true = () => parsed.ShouldBeTrue();
    }
}
