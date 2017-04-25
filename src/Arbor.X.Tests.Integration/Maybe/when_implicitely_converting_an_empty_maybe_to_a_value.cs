using System;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_implicitely_converting_an_empty_maybe_to_a_value
    {
        private static string value;
        private Because of = () => exception = Catch.Exception(() => value = new Defensive.Maybe<string>());

        private It should_throw_an_exception = () => exception.ShouldNotBeNull();

        private static Exception exception;
    }
}
