﻿using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.WellknownVariables
{
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_listing_all_wellknown_variables
    {
        static IReadOnlyCollection<VariableDescription> readOnlyCollection;

        Because of = () => readOnlyCollection = WellKnownVariables.AllVariables;

        It should_contain_nested_class_constants = () =>
        {
            readOnlyCollection
                .Any(variable => variable.InvariantName.Equals(WellKnownVariables.TeamCityVcsNumber, StringComparison.OrdinalIgnoreCase))
                .ShouldBeTrue();
        };

        It should_contain_non_nested_class_constants = () =>
        {
            readOnlyCollection
                .Any(variable => variable.InvariantName.Equals(WellKnownVariables.ExternalTools_NuGetServer_Enabled, StringComparison.OrdinalIgnoreCase))
                .ShouldBeTrue();
        };

        It should_print_all_variables = () =>
        {
            foreach (VariableDescription variableDescription in readOnlyCollection)
            {
                Console.WriteLine(variableDescription.ToString());
            }
        };
    }
}
