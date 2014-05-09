using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.BoolExtensions
{
    [Subject(typeof(Core.BoolExtensions))]
    public class when_parsing_null_value_with_default_true
    {
        static bool result;
        Because of = () => { result = ((string) null).TryParseBool(defaultValue:true); };

        It should_be_true = () => result.ShouldBeTrue();
    }
}