using Arbor.Build.Core.Tools.Git;
using JetBrains.Annotations;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.Tools.Git.GitBranchNameExtensions))]
    public class when_getting_branch_name_head
    {
        [CanBeNull] static string result;

        static string name;

        Establish context = () => name = "HEAD";

        Because of = () => result = name.GetBranchName();

        It should_not_return_a_branch_name = () => result.ShouldBeNull();
    }
}
