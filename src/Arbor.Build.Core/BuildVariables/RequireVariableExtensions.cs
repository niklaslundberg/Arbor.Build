using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.Exceptions;

namespace Arbor.Build.Core.BuildVariables;

public static class RequireVariableExtensions
{
    public static IVariable Require(this IReadOnlyCollection<IVariable> variables, string variableName)
    {
        ArgumentNullException.ThrowIfNull(variables);

        if (string.IsNullOrWhiteSpace(variableName))
        {
            throw new ArgumentNullException(nameof(variableName));
        }

        var foundVariables = variables
            .Where(var => var.Key.Equals(variableName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foundVariables.Count > 1)
        {
            throw new InvalidOperationException(
                $"The are multiple variables with key '{variableName}'");
        }

        IVariable? variable = foundVariables.SingleOrDefault();

        if (variable is null)
        {
            string message = $"The key '{variableName}' was not found in the variable collection";
            VariableDescription? property = WellKnownVariables.AllVariables.SingleOrDefault(
                item => item.InvariantName.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (property is {})
            {
                message +=
                    $". (The variable is a well-known property {typeof(WellKnownVariables)}.{property.WellKnownName})";
            }

            throw new BuildException(message, variables);
        }

        return variable;
    }
}