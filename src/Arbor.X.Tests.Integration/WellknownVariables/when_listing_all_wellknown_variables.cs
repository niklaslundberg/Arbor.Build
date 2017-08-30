using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.BuildVariables;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.WellknownVariables
{
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_listing_all_wellknown_variables
    {
        private static IReadOnlyCollection<VariableDescription> readOnlyCollection;

        private Because of = () => { readOnlyCollection = WellKnownVariables.AllVariables; };

        private It should_print_all_variables = () =>
        {
            foreach (VariableDescription variableDescription in readOnlyCollection)
            {
                Console.WriteLine(variableDescription.ToString());
            }
        };
        private It should_contain_nested_class_constants = () =>
        {
            readOnlyCollection.Any(variable => variable.InvariantName.Equals(WellKnownVariables.TeamCity.TeamCityVcsNumber)).ShouldBeTrue();
        };
    }
}
