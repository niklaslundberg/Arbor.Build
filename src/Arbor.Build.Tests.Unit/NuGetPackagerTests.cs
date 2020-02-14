using Arbor.Build.Core.Tools.NuGet;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class NuGetPackagerTests
    {
        [InlineData("feature-123")]
        [InlineData("any-branch")]
        [InlineData("release")]
        [InlineData("master")]
        [InlineData("develop")]
        [Theory]
        public void WhenBranchNameIsDisabled(string branchName)
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", false, branchName, false);
            Assert.Equal("MyPackage", id);
        }

        [InlineData("any-branch")]
        [Theory]
        public void WhenBranchNameIsEnabled(string branchName)
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", false, branchName, true);
            Assert.Equal($"MyPackage-{branchName}", id);
        }

        [InlineData("any-branch")]
        [Theory]
        public void WhenBranchNameIs2Enabled(string branchName)
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", false, branchName, true, ".Environment.Test");
            Assert.Equal($"MyPackage-{branchName}.Environment.Test", id);
        }

        [InlineData("any-branch")]
        [InlineData("release")]
        [Theory]
        public void WhenBranchNameIs2Disabled(string branchName)
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", false, branchName, false, ".Environment.Test");
            Assert.Equal("MyPackage.Environment.Test", id);
        }

        [InlineData("feature", "123")]
        [Theory]
        public void WhenBranchNameIsEnabledForFeatureBranches(string branchPrefix, string branchSpecificName)
        {
            string branchName = $"{branchPrefix}-{branchSpecificName}";
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", false, branchName, true);
            Assert.Equal($"MyPackage-{branchSpecificName}", id);
        }

        [Fact]
        public void WhenBranchNameIsDisabledAndReleaseTrue()
        {
            string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", true, "feature-123", false);
            Assert.Equal("MyPackage", id);
        }
    }
}