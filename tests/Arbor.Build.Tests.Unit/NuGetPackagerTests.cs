using Arbor.Build.Core.Tools.NuGet;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class NuGetPackagerTests
    {
        [Fact]
        public void WhenBranchNameIs2Disabled()
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", ".Environment.Test");
            Assert.Equal("MyPackage.Environment.Test", id);
        }

        [Fact]
        public void WhenBranchNameIsDisabledAndReleaseTrue()
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage");
            Assert.Equal("MyPackage", id);
        }
    }
}