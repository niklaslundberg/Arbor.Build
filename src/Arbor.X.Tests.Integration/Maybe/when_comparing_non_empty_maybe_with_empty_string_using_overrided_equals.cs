using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_empty_string_using_overrided_equals
    {
        Because of = () => equal = new Maybe<string>("a string").Equals("");

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}