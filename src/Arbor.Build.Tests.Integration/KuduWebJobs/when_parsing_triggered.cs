using Arbor.Build.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.KuduWebJobs
{
    [Subject(typeof(KuduWebJobType))]
    public class when_parsing_triggered
    {
        static KuduWebJobType parsed;

        Because of = () => parsed = KuduWebJobType.Parse("triggered");

        It should_return_a_valid_type = () => parsed.ShouldEqual(KuduWebJobType.Triggered);
    }
}
