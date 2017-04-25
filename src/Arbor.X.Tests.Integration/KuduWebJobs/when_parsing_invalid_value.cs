using System;
using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebJobs
{
    [Subject(typeof(KuduWebJobType))]
    public class when_parsing_invalid_value
    {
        private static Exception exception;
        private Because of = () => { exception = Catch.Exception(() => KuduWebJobType.Parse("blahablaha")); };

        private It should_throw_a_format_exception = () => exception.ShouldBeOfExactType<FormatException>();
    }
}
