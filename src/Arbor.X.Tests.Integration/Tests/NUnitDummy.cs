using NUnit.Framework;

namespace Arbor.Build.Tests.Integration.Tests
{
    [TestFixture]
    public class NUnitDummy
    {
        [Test]
        public void DoNothing()
        {
            Assert.That(true, "This is a dummy test for NUnit");
        }
    }
}
