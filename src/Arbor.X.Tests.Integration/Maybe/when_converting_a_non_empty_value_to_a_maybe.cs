using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_converting_a_non_empty_value_to_a_maybe
    {
        private Because of = () => maybe = "a string";

        private It should_have_a_value = () => maybe.HasValue.ShouldBeTrue();

        private It should_have_value_equal_to_original = () => maybe.Value.ShouldEqual("a string");

        private static Defensive.Maybe<string> maybe;
    }
}