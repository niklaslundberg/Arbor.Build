using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_converting_a_maybe_to_a_value
    {
        static string string_value;
        Because of = () => string_value = new Defensive.Maybe<string>("a string");

        It should_return_false = () => string_value.ShouldEqual("a string");
    }
}
