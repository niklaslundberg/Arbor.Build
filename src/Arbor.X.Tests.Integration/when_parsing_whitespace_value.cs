using System;
using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration
{
    [Subject(typeof (KuduWebJobType))]
    public class when_parsing_whitespace_value
    {
        static Exception exception;
        Because of = () => { exception = Catch.Exception(() => KuduWebJobType.Parse("\t")); };

        It should_throw_a_format_exception = () => exception.ShouldBeOfExactType<ArgumentNullException>();
    }
}