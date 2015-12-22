using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_maybe_with_different_type_of_maybe
    {
        Because of = () => equal = Equals(new Maybe<string>("a string"), new Maybe<object>(42));

        It should_return_false = () => equal.ShouldBeFalse();

        static bool equal;
    }
}