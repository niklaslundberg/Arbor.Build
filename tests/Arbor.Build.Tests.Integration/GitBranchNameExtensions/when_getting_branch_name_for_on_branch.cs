using Arbor.Build.Core.Tools.Git;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranchNameExtensions;

[Subject(typeof(Core.Tools.Git.GitBranchNameExtensions))]
public class when_getting_branch_name_for_on_branch
{
    static string? result;

    static string? name;

    Establish context = () => name = "On branch develop";

    Because of = () => result = name!.GetBranchName();

    It should_find_the_branch_name = () => result.ShouldNotBeNull();

    It should_have_branch_name_develop = () => result.ShouldEqual("develop");
}