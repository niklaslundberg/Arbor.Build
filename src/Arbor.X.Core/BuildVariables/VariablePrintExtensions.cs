using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arbor.X.Core.GenericExtensions;

namespace Arbor.X.Core.BuildVariables
{
    public static class VariablePrintExtensions
    {
        public static string Print(this IEnumerable<IVariable> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            var dictionaries =
                variables.Select(
                    variable => new Dictionary<string, string> {{"Name", variable.Key}, {"Value", variable.Value}});

            return dictionaries.DisplayAsTable();
        }

        public static string DisplayValue(this IVariable variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }
            return string.Format("\t{0}: {1}", variable.Key, variable.Value);
        }
    }
}