using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Arbor.X.Tests.Integration.Tests
{
    [TestClass]
    public class VSTestDummy
    {
        [TestMethod]
        public void DoNothing()
        {
            Assert.IsTrue(true, "This is a dummy test for VSTest");
        }
    }
}