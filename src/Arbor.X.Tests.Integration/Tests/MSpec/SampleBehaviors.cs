using System;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Behaviors]
    public class SampleBehaviors
    {
        protected static Boolean Result;

        private It should_satisfy_the_specification = () =>
            Result.ShouldBeTrue();
    }
}