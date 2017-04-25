using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_value_using_equals_operator
    {
        private Because of = () => equal = new Defensive.Maybe<string>("a string") == "a string";

        private It should_return_true = () => equal.ShouldBeTrue();

        private static bool equal;
    }
}
