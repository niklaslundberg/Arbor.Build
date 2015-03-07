using System;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_gettimg_the_item_from__an_empty_maybe
    {
        Because of = () => exception = Catch.Exception(() => new Maybe<string>().Item);

        It should_throw_an_exception = () => exception.ShouldNotBeNull();

        static Exception exception;
    }
}