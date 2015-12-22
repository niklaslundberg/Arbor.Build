using System;

using Arbor.Defensive;
using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Maybe
{
    public class when_explicitely_converting_an_empty_maybe_to_a_value
    {
        Because of = () => exception = Catch.Exception(() => (string) new Maybe<string>());

        It should_throw_an_exception = () => exception.ShouldNotBeNull();

        static Exception exception;
    }
}