using Xunit;

namespace Arbor.Build.Tests.Integration.Tests
{
    public class XunitDummy
    {
        [Fact]
        public void DoNothing() => Assert.True(true);
    }
}
