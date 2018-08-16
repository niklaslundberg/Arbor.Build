using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_empty_maybe_using_not_equals_operator
    {
        static bool equal;
        Because of = () => equal = new Defensive.Maybe<string>("a string") != default(Defensive.Maybe<string>);

        It should_return_true = () => equal.ShouldBeTrue();
    }
}
