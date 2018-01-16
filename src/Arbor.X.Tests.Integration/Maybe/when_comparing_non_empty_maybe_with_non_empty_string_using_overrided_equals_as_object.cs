using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_non_empty_string_using_overrided_equals_as_object
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>("a string").Equals((object)"a string");

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
