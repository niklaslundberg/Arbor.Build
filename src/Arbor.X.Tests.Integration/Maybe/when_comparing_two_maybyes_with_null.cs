using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_maybyes_with_null
    {
        Because of = () => equal = Equals(new Defensive.Maybe<string>(), new Defensive.Maybe<string>());

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}