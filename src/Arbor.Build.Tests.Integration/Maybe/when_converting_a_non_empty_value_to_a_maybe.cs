using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_converting_a_non_empty_value_to_a_maybe
    {
        static Defensive.Maybe<string> maybe;
        Because of = () => maybe = "a string";

        It should_have_a_value = () => maybe.HasValue.ShouldBeTrue();

        It should_have_value_equal_to_original = () => maybe.Value.ShouldEqual("a string");
    }
}
