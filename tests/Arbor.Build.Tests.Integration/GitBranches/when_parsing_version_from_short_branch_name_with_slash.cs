using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranches;

[Subject(typeof(Branch))]
public class when_parsing_version_from_short_branch_name_with_slash
{
    static string branchName;
    static string? version;
    Establish context = () => branchName = "release/1.2.3";

    Because of = () => version = Branch.BranchSemVerMajorMinorPatch(branchName, EnvironmentVariables.Empty)?.ToString();

    It should_extract_the_version = () => version.ShouldEqual("1.2.3");
}