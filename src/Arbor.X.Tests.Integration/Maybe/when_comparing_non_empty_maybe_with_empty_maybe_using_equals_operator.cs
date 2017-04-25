using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_empty_maybe_using_equals_operator
    {
        private Because of = () => equal = new Defensive.Maybe<string>("a string") == default(Defensive.Maybe<string>);

        private It should_return_false = () => equal.ShouldBeFalse();

        private static bool equal;
    }
}