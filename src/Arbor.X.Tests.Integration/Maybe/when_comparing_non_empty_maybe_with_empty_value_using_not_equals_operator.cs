using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_empty_value_using_not_equals_operator
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>("a string") != (string)null;

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
