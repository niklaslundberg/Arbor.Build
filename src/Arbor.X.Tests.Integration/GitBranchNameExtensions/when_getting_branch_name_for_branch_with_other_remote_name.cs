using Arbor.X.Core;

using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_branch_with_other_remote_name
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        private Establish context = () => { name = "## develop...origin/otherremodevelop"; };

        private Because of = () => { result = name.GetBranchName(); };

        private It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }
}