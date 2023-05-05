using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.GenericExtensions;

namespace Arbor.Build.Core.BuildVariables;

public static class VariablePrintExtensions
{
    public static string Print(this IEnumerable<IVariable> variables)
    {
        if (variables == null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        IEnumerable<Dictionary<string, string?>> dictionaries =
            variables.Select(
                variable => new Dictionary<string, string?>
                {
                    { "Name", variable.Key },
                    { "Value", variable.Key.GetDisplayValue(variable.Value) }
                });

        return dictionaries.DisplayAsTable();
    }

    private static readonly string[] SensitiveValues = { "password", "apikey", "username", "pw", "token", "jwt", "connectionstring" };

    public static string DisplayPair(this IVariable variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        string? value = GetDisplayValue(variable.Key, variable.Value);

        return $"\t{variable.Key}: {value}";
    }

    public static string? GetDisplayValue(this string key, string? value)
    {
        if (SensitiveValues.Any(sensitive =>
            {
                var comparisonType = StringComparison.OrdinalIgnoreCase;

                return key
                    .Replace("-", "", comparisonType)
                    .Replace("_", "", comparisonType)
                    .Replace(".", "", comparisonType)
                    .Contains(sensitive, comparisonType);
            }))
        {
            value = "*****";
        }

        return value;
    }
}