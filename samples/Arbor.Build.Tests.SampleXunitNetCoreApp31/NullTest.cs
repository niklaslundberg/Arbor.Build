using Xunit;

namespace Arbor.Build.Sample.Tests.XunitNet6;

public class NullTest
{
    [Fact]
    public void ShouldBeTrue() => Assert.True(true);
}