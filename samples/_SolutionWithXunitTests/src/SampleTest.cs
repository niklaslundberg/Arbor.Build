using Xunit;

namespace ProjectWithXunitTests
{
    public class SampleTest
    {
        [Fact]
        public void DummyTest() => Assert.True(true);
    }
}