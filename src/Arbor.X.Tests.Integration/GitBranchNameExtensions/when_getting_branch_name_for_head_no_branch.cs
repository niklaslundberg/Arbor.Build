using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_head_no_branch
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "## HEAD (no branch)"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_head
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "HEAD"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }
}
