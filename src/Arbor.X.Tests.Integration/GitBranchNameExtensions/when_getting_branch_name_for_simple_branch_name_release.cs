using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.Tools.Git.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_release
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "release"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        It should_have_branch_name_develop = () => result.Value.ShouldEqual("release");
    }
}
