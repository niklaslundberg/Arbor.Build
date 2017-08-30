using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_equal_maybes_with_equals_operator
    {
        private static bool equal;

        private Because of = () => equal = new Defensive.Maybe<string>("a string") ==
                                           new Defensive.Maybe<string>("a string");

        private It should_return_true = () => equal.ShouldBeTrue();
    }
}
