using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_converting_a_maybe_to_a_value
    {
        Because of = () => string_value = new Maybe<string>("a string");

        It should_return_false = () => string_value.ShouldEqual("a string");

        static string string_value;
    }
}