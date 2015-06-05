using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_two_equal_maybes_with_equals_operator
    {
        Because of = () => equal = new Arbor.X.Core.Maybe<string>("a string") == new Core.Maybe<string>("a string");

        It should_return_true = () => equal.ShouldBeTrue();

        static bool equal;
    }
}