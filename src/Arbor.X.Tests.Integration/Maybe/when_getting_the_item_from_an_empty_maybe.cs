using System;

using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_getting_the_item_from_an_empty_maybe
    {
        static Exception exception;

        Because of = () => exception = Catch.Exception(() => new Maybe<string>().Value);

        It should_throw_an_exception = () => exception.ShouldNotBeNull();
    }
}