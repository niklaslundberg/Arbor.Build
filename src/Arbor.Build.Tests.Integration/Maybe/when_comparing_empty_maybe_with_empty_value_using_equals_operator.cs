using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_empty_maybe_with_empty_value_using_equals_operator
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>() == (string)null;

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
