using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_empty_maybe_with_non_empty_maybe_using_equals_override
    {
        Because of = () => equal = new Maybe<string>().Equals(new Maybe<string>("a string"));

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}