using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_a_non_empty_maybye_with_an_empty_value
    {
        static bool equal;

        Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"),
            new Defensive.Maybe<string>());

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
