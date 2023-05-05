using Arbor.Build.Core.Tools.Git;
using Xunit;

namespace Arbor.Build.Tests.Integration.GitBranches;

public class CheckStatusForReleaseBranchWithVersion
{
    [Fact]
    public void ShouldBeProductionBranch()
    {
        var branchName = new BranchName("release-v3.0");

        Assert.False(branchName.IsDevelopBranch());
        Assert.False(branchName.IsFeatureBranch());
        Assert.True(branchName.IsProductionBranch());
    }
}