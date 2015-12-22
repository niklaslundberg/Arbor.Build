using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_maybyes_with_equal_non_null
    {
        Because of = () => equal = Equals(new Maybe<string>("a string"), new Maybe<string>("a string"));

        It should_return_true = () => equal.ShouldBeTrue();

        static bool equal;
    }
}