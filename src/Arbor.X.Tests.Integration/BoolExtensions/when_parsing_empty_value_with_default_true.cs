using Arbor.X.Core.GenericExtensions;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.GenericExtensions.BoolExtensions))]
    public class when_parsing_empty_value_with_default_true
    {
        static bool result;
        Because of = () => { result = string.Empty.TryParseBool(true); };

        It should_be_true = () => result.ShouldBeTrue();
    }
}
