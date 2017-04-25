using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_maybe_with_different_type_of_maybe
    {
        private Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"),
            new Defensive.Maybe<object>(42));

        private It should_return_false = () => equal.ShouldBeFalse();

        private static bool equal;
    }
}
