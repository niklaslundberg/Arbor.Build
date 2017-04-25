using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_default_maybes
    {
        private Because of = () => equal = Equals(default(Defensive.Maybe<string>), default(Defensive.Maybe<string>));

        private It should_return_false = () => equal.ShouldBeFalse();

        private static bool equal;
    }
}