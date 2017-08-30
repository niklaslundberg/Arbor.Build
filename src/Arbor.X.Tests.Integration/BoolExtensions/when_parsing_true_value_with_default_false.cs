using Arbor.X.Core.GenericExtensions;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.BoolExtensions))]
    public class when_parsing_true_value_with_default_false
    {
        private static bool result;
        private Because of = () => { result = "true".TryParseBool(false); };

        private It should_be_true = () => result.ShouldBeTrue();
    }
}
