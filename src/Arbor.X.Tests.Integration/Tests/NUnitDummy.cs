using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Arbor.X.Tests.Integration.Tests
{
    [TestFixture]
    public class NUnitDummy
    {
        [Test]
        public void DoNothing()
        {
            Assert.IsTrue(true, "This is a dummy test for NUnit");
        }
    }
}
