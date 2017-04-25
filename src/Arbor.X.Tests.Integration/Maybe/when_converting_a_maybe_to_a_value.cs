using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_converting_a_maybe_to_a_value
    {
        private Because of = () => string_value = new Defensive.Maybe<string>("a string");

        private It should_return_false = () => string_value.ShouldEqual("a string");

        private static string string_value;
    }
}
