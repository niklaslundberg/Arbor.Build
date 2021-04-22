using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.NuGet;
using Xunit;

namespace Arbor.Build.Tests.Integration.Versioning
{
    public class NuGetVersionHelperTests
    {
        [Fact]
        public void Should()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4",
                false,
                "build",
                true,
                null,
                null,
                NuGetVersioningSettings.Default);

            Assert.Equal("1.2.3-build.4", version);
        }

        [Fact]
        public void FeatureBranchShouldBeUsedAsSuffix()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4",
                false,
                "build",
                true,
                null,
                null,
                NuGetVersioningSettings.Default, branchName: new BranchName("feature-abc"));

            Assert.Equal("1.2.3-build.4.abc", version);
        }

        [Theory]
        [InlineData("1.2.3", "main", "1.2.3")]
        [InlineData("1.2.3", "master", "1.2.3")]
        [InlineData("1.2.3.4", "develop", "1.2.3-build.4")]
        [InlineData("1.2.3.4", "refs/heads/myfeature", "1.2.3-build.4.myfeature")]
        public void KnownBranchShouldNotBeUsedAsSuffix(string baseVersion, string branchName, string expected)
        {
            string version = NuGetVersionHelper.GetVersion(baseVersion,
                new BranchName(branchName).IsProductionBranch(),
                "build",
                true,
                null,
                null,
                NuGetVersioningSettings.Default, branchName: new BranchName(branchName));

            Assert.Equal(expected, version);
        }
    }
}