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
#pragma warning disable MSTEST0032
    public void DoNothing() => Assert.IsTrue(true, "This is a dummy test for MSTest discovery validation");
#pragma warning restore MSTEST0032
}