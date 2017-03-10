using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_equal_maybes_with_not_equals_operator
    {
        Because of = () => equal = new Defensive.Maybe<string>("a string") != new Defensive.Maybe<string>("a string");

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}