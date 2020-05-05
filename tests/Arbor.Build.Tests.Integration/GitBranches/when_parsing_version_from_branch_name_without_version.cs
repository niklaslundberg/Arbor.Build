using Arbor.Build.Core;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Tests.Integration.Bootstrapper;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranches
{
    [Subject(typeof(BranchHelper))]
    public class when_parsing_version_from_branch_name_without_version
    {
        static string branchName;
        static string version;
        Establish context = () => branchName = "refs/heads/develop";

        Because of = () => version = BranchHelper.BranchSemVerMajorMinorPatch(branchName, EnvironmentVariables.Empty).ToString();

        It should_not_extract_the_version = () => version.ShouldEqual("0.0.0");
    }
}
