using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.WellknownVariables
{
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_listing_all_wellknown_variables_as_a_table
    {
        private static IReadOnlyCollection<VariableDescription> readOnlyCollection;

        private Because of = () => { readOnlyCollection = WellKnownVariables.AllVariables; };

        private It should_print = () =>
        {
            List<Dictionary<string, string>> dicts = readOnlyCollection
                .Select(variableDescription => new Dictionary<string, string>
                {
                    {
                        "Name",
                        variableDescription.InvariantName
                    },
                    {
                        "Description", variableDescription.Description
                    },
                    {
                        "Default value", variableDescription.DefaultValue
                    }
                })
                .ToList();

            try
            {
                string displayAsTable = dicts.DisplayAsTable();
                Console.WriteLine(displayAsTable);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        };
    }
}
