using Arbor.X.Core;

using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_branch_with_same_origin_name
    {
        private static Maybe<string> result;

        private static string name;

        Establish context = () => { name = "## develop...origin/develop"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }
}
