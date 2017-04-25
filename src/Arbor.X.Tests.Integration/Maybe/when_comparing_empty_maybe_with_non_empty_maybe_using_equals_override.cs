using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_empty_maybe_with_non_empty_maybe_using_equals_override
    {
        private Because of =
            () => equal = new Defensive.Maybe<string>().Equals(new Defensive.Maybe<string>("a string"));

        private It should_return_false = () => equal.ShouldBeFalse();

        private static bool equal;
    }
}
