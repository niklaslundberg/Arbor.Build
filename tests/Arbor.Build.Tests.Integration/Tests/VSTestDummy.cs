using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.Build.Tests.Integration.Tests;

[TestClass]
public class VSTestDummy
{
    [TestMethod]
    public void DoNothing() => Assert.IsTrue(true, "This is a dummy test for VSTest");
}