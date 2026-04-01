using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.Build.Tests.Integration.Tests;

/// <summary>
/// Sample test class demonstrating MSTest framework support for test discovery validation.
/// Real tests in this project use xUnit v3.
/// </summary>
[TestClass]
public class VSTestDummy
{
    [TestMethod]
    public void DoNothing() => Assert.IsTrue(true, "This is a dummy test for MSTest discovery validation");
}