using Arbor.X.Core;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_master
    {
        static Defensive.Maybe<string> result;

        static string name;

        Establish context = () => { name = "master"; };

        Because of = () => { result = name.GetBranchName(); };

        It should_find_the_branch_name = () => result.HasValue.ShouldBeTrue();

        It should_have_branch_name_develop = () => result.Value.ShouldEqual("master");
    }
}