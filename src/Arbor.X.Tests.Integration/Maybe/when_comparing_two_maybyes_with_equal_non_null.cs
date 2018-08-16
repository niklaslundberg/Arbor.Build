using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_two_maybyes_with_equal_non_null
    {
        static bool equal;

        Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"),
            new Defensive.Maybe<string>("a string"));

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
