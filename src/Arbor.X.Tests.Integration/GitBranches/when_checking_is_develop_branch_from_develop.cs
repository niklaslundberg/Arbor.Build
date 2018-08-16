using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranches
{
    [Subject(typeof(BranchHelper))]
    public class when_checking_is_develop_branch_from_develop
    {
        static BranchName branchName;
        static bool is_develop;
        Establish context = () => { branchName = new BranchName("refs/heads/develop"); };

        Because of = () => { is_develop = branchName.IsDevelopBranch(); };

        It should_extract_the_version = () => is_develop.ShouldBeFalse();
    }
}
