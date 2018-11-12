using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.GenericExtensions;

namespace Arbor.Build.Core.BuildVariables
{
    public static class VariablePrintExtensions
    {
        public static string Print(this IEnumerable<IVariable> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            IEnumerable<Dictionary<string, string>> dictionaries =
                variables.Select(
                    variable => new Dictionary<string, string>
                    {
                        { "Name", variable.Key },
                        { "Value", variable.Value }
                    });

            return dictionaries.DisplayAsTable();
        }

        public static string DisplayValue(this IVariable variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            return $"\t{variable.Key}: {variable.Value}";
        }
    }
}
