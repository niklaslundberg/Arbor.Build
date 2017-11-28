using System;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_implicitely_converting_an_empty_maybe_to_a_value
    {
        static string value;

        static Exception exception;
        Because of = () => exception = Catch.Exception(() => value = new Defensive.Maybe<string>());

        It should_throw_an_exception = () => exception.ShouldNotBeNull();
    }
}
