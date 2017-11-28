using Xunit;

namespace Arbor.X.Tests.Integration.Tests
{
    public class XunitDummy
    {
        [Fact]
        public void DoNothing()
        {
            Assert.True(true);
        }
    }
}
