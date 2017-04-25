using Arbor.X.Core.Tools.Kudu;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.KuduWebJobs
{
    [Subject(typeof(KuduWebJobType))]
    public class when_parsing_valid_lowercase
    {
        private static KuduWebJobType parsed;

        private Because of = () => { parsed = KuduWebJobType.Parse("continuous"); };

        private It should_return_a_valid_type = () => parsed.ShouldEqual(KuduWebJobType.Continuous);
    }
}
