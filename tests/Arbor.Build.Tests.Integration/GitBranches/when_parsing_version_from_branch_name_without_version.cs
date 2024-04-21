using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranches;

[Subject(typeof(Branch))]
public class when_parsing_version_from_branch_name_without_version
{
    static string branchName;
    static string version;
    Establish context = () => branchName = "refs/heads/develop";

    Because of = () => version = Branch.BranchSemVerMajorMinorPatch(branchName, EnvironmentVariables.Empty)!.ToString();

    It should_not_extract_the_version = () => version.ShouldEqual("0.0.0");
}