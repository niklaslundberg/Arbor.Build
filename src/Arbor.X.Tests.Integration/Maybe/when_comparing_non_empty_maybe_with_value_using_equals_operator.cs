using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_non_empty_maybe_with_value_using_equals_operator
    {
        Because of = () => equal = new Maybe<string>("a string") == "a string";

        It should_return_true = () => equal.ShouldBeTrue();

        static bool equal;
    }
}