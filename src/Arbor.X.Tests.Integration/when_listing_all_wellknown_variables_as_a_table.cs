using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration
{
    public class when_listing_all_wellknown_variables_as_a_table
    {
        static IReadOnlyCollection<VariableDescription> readOnlyCollection;

        Because of = () => { readOnlyCollection = WellKnownVariables.AllVariables; };

        It should_print = () =>
        {
            List<Dictionary<string, string>> dicts = readOnlyCollection.Select(variableDescription => new Dictionary<string, string>
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
                                                                                                      }).ToList();

            Console.WriteLine(dicts.DisplayAsTable());
        };
    }
}