using System;
using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebJobs
{
    [Subject(typeof(KuduWebJobType))]
    public class when_parsing_job_web_type_from_empty_value
    {
        private static Exception exception;
        private Because of = () => { exception = Catch.Exception(() => KuduWebJobType.Parse("")); };

        private It should_throw_a_format_exception = () => exception.ShouldBeOfExactType<ArgumentNullException>();
    }
}
