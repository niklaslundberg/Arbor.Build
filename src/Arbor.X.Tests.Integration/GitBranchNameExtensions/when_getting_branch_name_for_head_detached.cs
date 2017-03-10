using Arbor.X.Core;

using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_head_detached
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        Establish context = () => { name = "## HEAD detached"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_not_return_a_branch_name = () => result.HasValue.ShouldBeFalse();
    }
}