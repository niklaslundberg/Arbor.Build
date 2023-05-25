using Arbor.Build.Core.Tools.MSBuild;
using Xunit;

namespace Arbor.Build.Tests.Unit;

public class GitModelTests
{
    [Fact]
    public void ParseGitFlowMasterShouldReturnInstance()
    {
        bool parsed = GitBranchModel.TryParse("GitFlowBuildOnMaster", out var model);


        Assert.True(parsed);
        Assert.NotNull(model);
    }

    [Fact]
    public void ParseGitFlowMainShouldReturnInstance()
    {
        bool parsed = GitBranchModel.TryParse("GitFlowBuildOnMain", out var model);


        Assert.True(parsed);
        Assert.NotNull(model);
    }

    [Fact]
    public void ParseInvalidShouldReturnFalse()
    {
        bool parsed = GitBranchModel.TryParse("BadValue", out var model);

        Assert.False(parsed);
        Assert.Null(model);
    }
}