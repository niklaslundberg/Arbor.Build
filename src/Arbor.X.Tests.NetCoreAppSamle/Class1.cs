using System;
using Xunit;

namespace Arbor.X.Tests.NetCoreAppSamle
{
    public class SampleXunitTest
    {
        [Fact]
        public void AlwaysTrue()
        {
            Assert.True(true);
        }
    }
}
