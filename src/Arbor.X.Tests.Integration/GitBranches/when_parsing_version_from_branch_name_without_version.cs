using Arbor.X.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranches
{
    [Subject(typeof (BranchHelper))]
    public class when_parsing_version_from_branch_name_without_version
    {
        private static string branchName;
        private static string version;
        private Establish context = () => { branchName = "refs/heads/develop"; };

        private Because of = () => { version = BranchHelper.BranchSemVerMajorMinorPatch(branchName).ToString(); };

        private It should_not_extract_the_version = () => version.ShouldEqual("0.0.0");
    }
}