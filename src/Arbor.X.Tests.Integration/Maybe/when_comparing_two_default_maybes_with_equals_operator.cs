using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_default_maybes_with_equals_operator
    {
        static bool equal;
        Because of = () => equal = default == default(Defensive.Maybe<string>);

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
