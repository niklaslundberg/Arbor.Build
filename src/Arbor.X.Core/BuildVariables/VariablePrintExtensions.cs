using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            var sb = new StringBuilder();

            foreach (var variable in variables.OrderBy(@var => @var.Key))
            {
                sb.AppendLine(variable.DisplayValue());
            }

            return sb.ToString();
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