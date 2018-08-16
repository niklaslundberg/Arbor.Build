using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_two_equal_maybes_with_equals_operator
    {
        static bool equal;

        Because of = () => equal = new Defensive.Maybe<string>("a string") ==
                                   new Defensive.Maybe<string>("a string");

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
