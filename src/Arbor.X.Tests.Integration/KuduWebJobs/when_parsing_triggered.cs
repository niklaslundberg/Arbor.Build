using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebJobs
{
    [Subject(typeof (KuduWebJobType))]
    public class when_parsing_triggered
    {
        private static KuduWebJobType parsed;

        private Because of = () => { parsed = KuduWebJobType.Parse("triggered"); };

        private It should_return_a_valid_type = () => parsed.ShouldEqual(KuduWebJobType.Triggered);
    }
}