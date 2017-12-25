using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_on_branch
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "On branch develop"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_release
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "release"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        It should_have_branch_name_develop = () => result.Value.ShouldEqual("release");
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_develop
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "develop"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }
}
