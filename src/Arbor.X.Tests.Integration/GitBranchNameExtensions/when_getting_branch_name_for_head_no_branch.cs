using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_head_no_branch
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        private Establish context = () => { name = "## HEAD (no branch)"; };

        private Because of = () => { result = name.GetBranchName(); };

        private It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_head
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        private Establish context = () => { name = "HEAD"; };

        private Because of = () => { result = name.GetBranchName(); };

        private It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }
}
