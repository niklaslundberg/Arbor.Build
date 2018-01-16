using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_null_object_using_overrided_equals
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>("a string").Equals((object)null);

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
