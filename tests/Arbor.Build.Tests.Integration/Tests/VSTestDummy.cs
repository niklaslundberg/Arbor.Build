using Xunit;

namespace Arbor.Build.Tests.Integration.Tests;

public class VSTestDummy
{
    [Fact]
    public void DoNothing() => Assert.True(true, "This is a dummy test for xUnit discovery validation");
}