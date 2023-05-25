﻿using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.WellknownVariables;

[Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
public class when_listing_all_wellknown_variables_as_a_table
{
    static IReadOnlyCollection<VariableDescription> readOnlyCollection;

    Because of = () => readOnlyCollection = WellKnownVariables.AllVariables;

    It should_print = () =>
    {
        var dicts = readOnlyCollection
            .Select(variableDescription => new Dictionary<string, string?>
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