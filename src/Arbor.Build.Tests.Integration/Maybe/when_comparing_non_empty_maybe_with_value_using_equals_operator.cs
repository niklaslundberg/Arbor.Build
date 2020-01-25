using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_value_using_equals_operator
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>("a string") == "a string";

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
