using Xunit;

namespace Arbor.X.Tests.NetCoreAppSample
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
