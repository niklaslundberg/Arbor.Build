using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Tags("dummyexcluded")]
    [Subject(typeof(object))]
    public class when_having_an_excluded_tag
    {
        private Establish context = () => { };

        private Because of = () => { };

        private It should_not_be_run = () => { };
    }
}
