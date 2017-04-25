using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_default_maybes_with_not_equals_operator
    {
        private Because of = () => equal = default(Defensive.Maybe<string>) != default(Defensive.Maybe<string>);

        private It should_return_true = () => equal.ShouldBeTrue();

        private static bool equal;
    }
}
