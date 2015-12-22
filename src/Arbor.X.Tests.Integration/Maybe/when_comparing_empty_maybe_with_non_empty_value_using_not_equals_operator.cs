using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_empty_maybe_with_non_empty_value_using_not_equals_operator
    {
        Because of = () => equal = new Maybe<string>() != "a string";

        It should_return_true = () => equal.ShouldBeTrue();

        static bool equal;
    }
}