using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_maybyes_with_equal_non_null
    {
        private Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"), new Defensive.Maybe<string>("a string"));

        private It should_return_true = () => equal.ShouldBeTrue();

        private static bool equal;
    }
}