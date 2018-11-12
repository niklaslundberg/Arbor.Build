using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_null
    {
        static bool equal;
        Because of = () => equal = Equals(new Defensive.Maybe<string>("a string"), null);

        It should_return_false = () => equal.ShouldBeFalse();
    }
}
