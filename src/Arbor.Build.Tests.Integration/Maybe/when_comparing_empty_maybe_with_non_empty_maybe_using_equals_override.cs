using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_empty_maybe_with_non_empty_maybe_using_equals_override
    {
        static bool equal;

        Because of =
            () => equal = new Defensive.Maybe<string>().Equals(new Defensive.Maybe<string>("a string"));

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
