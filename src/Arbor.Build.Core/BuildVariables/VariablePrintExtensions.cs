using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.GenericExtensions;

namespace Arbor.Build.Core.BuildVariables;

public static class VariablePrintExtensions
{
    public static string Print(this IEnumerable<IVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        IEnumerable<Dictionary<string, string?>> dictionaries =
            variables.Select(
                variable => new Dictionary<string, string?>
                {
                    { "Name", variable.Key },
                    { "Value", variable.Key.GetDisplayValue(variable.Value) }
                });

        return dictionaries.DisplayAsTable();
    }

    private static readonly FrozenSet<string> SensitiveValues = new[] { "password", "apikey", "username", "pw", "token", "jwt", "connectionString", "client_secret" }.ToFrozenSet();
    private static StringComparison _comparisonType;

    public static string DisplayPair(this IVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        string? value = GetDisplayValue(variable.Key, variable.Value);

        return $"\t{variable.Key}: {value}";
    }

    public static string? GetDisplayValue(this string key, string? value)
    {
        if (SensitiveValues.Any(sensitive =>
            {
                _comparisonType = StringComparison.OrdinalIgnoreCase;

                return key
                    .Replace("-", "", _comparisonType)
                    .Replace("_", "", _comparisonType)
                    .Replace(".", "", _comparisonType)
                    .Contains(sensitive, _comparisonType);
            }))
        {
            value = "*****";
        }

        return value;
    }
}