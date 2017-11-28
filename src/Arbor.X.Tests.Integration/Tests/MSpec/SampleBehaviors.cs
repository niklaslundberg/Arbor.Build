using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Behaviors]
    public class SampleBehaviors
    {
        protected static bool Result;

        It should_satisfy_the_specification = () =>
            Result.ShouldBeTrue();
    }
}
