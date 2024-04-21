using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;
using Xunit;

namespace Arbor.Build.Tests.Integration.GitBranches;

public class BranchHelperTests
{
    [Fact]
    public void ParseVersionFromDependabotBranch()
    {
        var semanticVersion = Branch.BranchSemVerMajorMinorPatch("refs/heads/dependabot/nuget/", EnvironmentVariables.Empty);

        semanticVersion.ShouldBeNull();
    }

    [Fact]
    public void ParseVersionBranchShouldReturnVersion()
    {
        var semanticVersion = Branch.BranchSemVerMajorMinorPatch("refs/heads/somebranch-1.2.3", EnvironmentVariables.Empty);

        semanticVersion.ShouldNotBeNull();

        semanticVersion!.Major.ShouldEqual(1);
        semanticVersion!.Minor.ShouldEqual(2);
        semanticVersion!.Patch.ShouldEqual(3);
    }
}