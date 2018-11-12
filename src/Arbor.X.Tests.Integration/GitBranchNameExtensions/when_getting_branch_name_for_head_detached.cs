using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.Tools.Git.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_head_detached
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => name = "## HEAD detached";

        Because of = () => result = name.GetBranchName();

        It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }
}
