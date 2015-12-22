using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_non_null_object_using_overrided_equals
    {
        Because of = () => equal = new Maybe<string>("a string").Equals(new object());

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}