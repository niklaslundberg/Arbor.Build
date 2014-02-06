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
                throw new ArgumentNullException("variable");
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                throw new ValidationException(string.Format("The variable {0} must have a non-empty value", variable.Key));
            }

            return variable;
        }
    }
}