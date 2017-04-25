using Arbor.X.Core.GenericExtensions;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.BoolExtensions))]
    public class when_parsing_false_value_with_default_true
    {
        private static bool result;
        private Because of = () => { result = "false".TryParseBool(true); };

        private It should_be_false = () => result.ShouldBeFalse();
    }
}
