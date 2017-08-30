using System;
using System.ComponentModel.DataAnnotations;

namespace Arbor.X.Core.BuildVariables
{
    public static class VariableValidationExtensions
    {
        public static IVariable ThrowIfEmptyValue(this IVariable variable)
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
    }
}
