using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_a_non_empty_maybye_with_an_empty_value
    {
        private Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"), new Defensive.Maybe<string>());

        private It should_return_false = () => equal.ShouldBeFalse();

        private static bool equal;
    }
}