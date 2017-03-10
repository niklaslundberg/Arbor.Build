using Arbor.X.Core;

using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_on_branch
    {
        private static Arbor.Defensive.Maybe<string> result;

        private static string name;

        Establish context = () => { name = "On branch develop"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_master
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        Establish context = () => { name = "master"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("master");
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_release
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        Establish context = () => { name = "release"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("release");
    }

    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_develop
    {
        private static Defensive.Maybe<string> result;

        private static string name;

        Establish context = () => { name = "develop"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        private It should_have_branch_name_develop = () => result.Value.ShouldEqual("develop");
    }
}
