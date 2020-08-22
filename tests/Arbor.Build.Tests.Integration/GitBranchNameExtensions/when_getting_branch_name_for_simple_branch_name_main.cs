﻿using Arbor.Build.Core.Tools.Git;
using JetBrains.Annotations;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.GitBranchNameExtensions
{
    [Subject(typeof(Core.Tools.Git.GitBranchNameExtensions))]
    public class when_getting_branch_name_for_simple_branch_name_main
    {
        [CanBeNull] static string result;

        static string name;

        Establish context = () => name = "main";

        Because of = () => result = name.GetBranchName();

        It should_find_the_branch_name = () => result.ShouldNotBeNull();

        It should_have_branch_name_develop = () => result.ShouldEqual("main");
    }
}