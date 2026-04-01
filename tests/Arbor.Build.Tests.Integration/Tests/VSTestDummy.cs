using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.Build.Tests.Integration.Tests;

[TestClass]
public class VSTestDummy
{
    [TestMethod]
#pragma warning disable MSTEST0032 // Dummy test for VSTest discovery validation
    public void DoNothing() => Assert.IsTrue(true, "This is a dummy test for VSTest");
#pragma warning restore MSTEST0032
}