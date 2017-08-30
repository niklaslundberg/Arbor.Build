using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
#pragma warning disable 1718

    [Subject(typeof(Defensive.Maybe<string>))]
    public class when_comparing_the_same_maybe_with_itself_with_not_equals_operator
    {
        private static bool equal;
        private static Defensive.Maybe<string> instance;
        private Establish context = () => instance = new Defensive.Maybe<string>("a string");

        // ReSharper disable once EqualExpressionComparison
        private Because of = () => equal = instance != instance;

        private It should_return_false = () => equal.ShouldBeFalse();
    }

#pragma warning restore 1718
}
