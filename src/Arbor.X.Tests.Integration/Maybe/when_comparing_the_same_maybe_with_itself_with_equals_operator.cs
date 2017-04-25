using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
#pragma warning disable 1718

    [Subject(typeof(Defensive.Maybe<string>))]
    public class when_comparing_the_same_maybe_with_itself_with_equals_operator
    {
        private Establish context = () => instance = new Defensive.Maybe<string>("a string");

        // ReSharper disable once EqualExpressionComparison
        private Because of = () => equal = instance == instance;

        private It should_return_true = () => equal.ShouldBeTrue();

        private static bool equal;
        private static Defensive.Maybe<string> instance;
    }

#pragma warning restore 1718
}
