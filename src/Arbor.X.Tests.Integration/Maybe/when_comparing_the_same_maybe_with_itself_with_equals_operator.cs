using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_comparing_the_same_maybe_with_itself_with_equals_operator
    {
        Establish context = () => instance = new Core.Maybe<string>("a string");

        // ReSharper disable once EqualExpressionComparison
        Because of = () => equal = instance == instance;

        It should_return_true = () => equal.ShouldBeTrue();

        static bool equal;
        static Maybe<string> instance;
    }
}