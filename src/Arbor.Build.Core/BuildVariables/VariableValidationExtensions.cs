using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Arbor.Build.Core.BuildVariables;

public static class VariableValidationExtensions
{
    public static IVariable ThrowIfEmptyValue(
        [NotNullIfNotNull("variable")] this IVariable variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (string.IsNullOrWhiteSpace(variable.Value))
        {
            throw new ValidationException($"The variable {variable.Key} must have a non-empty value");
        }

        return variable;
    }

    public static string GetValueOrThrow(
        [NotNullIfNotNull("variable")] this IVariable variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (string.IsNullOrWhiteSpace(variable.Value))
        {
            throw new ValidationException($"The variable {variable.Key} must have a non-empty value");
        }

        return variable.Value;
    }
}