using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_default_maybes
    {
        static bool equal;
        Because of = () => equal = Equals(default(Defensive.Maybe<string>), default(Defensive.Maybe<string>));

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
