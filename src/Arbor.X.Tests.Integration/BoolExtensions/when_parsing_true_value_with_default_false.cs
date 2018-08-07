using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.GenericExtensions.Boolean;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.Boolean.BoolExtensions))]
    public class when_parsing_true_value_with_default_false
    {
        static bool parsed;
        Because of = () => { parsed = "true".TryParseBool(out bool result, false); };

        It should_be_true = () => parsed.ShouldBeTrue();
    }
}
