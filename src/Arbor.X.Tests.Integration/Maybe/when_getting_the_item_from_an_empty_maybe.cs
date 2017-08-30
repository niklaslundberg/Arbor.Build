using System;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_getting_the_item_from_an_empty_maybe
    {
        private static Exception exception;

        private Because of = () => exception = Catch.Exception(() => new Defensive.Maybe<string>().Value);

        private It should_throw_an_exception = () => exception.ShouldNotBeNull();
    }
}
