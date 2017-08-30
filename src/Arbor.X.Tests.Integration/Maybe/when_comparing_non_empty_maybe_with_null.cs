using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_null
    {
        private static bool equal;
        private Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"), null);

        private It should_return_false = () => equal.ShouldBeFalse();
    }
}
