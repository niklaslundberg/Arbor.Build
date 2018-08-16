using System;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.Maybe
{
    public class when_getting_the_item_from_an_empty_maybe
    {
        static Exception exception;

        Because of = () => exception = Catch.Exception(() => new Defensive.Maybe<string>().Value);

        It should_throw_an_exception = () => exception.ShouldNotBeNull();
    }
}
