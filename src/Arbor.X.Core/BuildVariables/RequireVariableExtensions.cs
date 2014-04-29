using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.BuildVariables
{
    public static class RequireVariableExtensions
    {
        public static IVariable Require(this IReadOnlyCollection<IVariable> variables, string variableName)
        {
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentNullException("variableName");
            }

            var variable = variables.SingleOrDefault(@var => @var.Key.Equals(variableName, StringComparison.InvariantCultureIgnoreCase));

            if (variable == null)
            {
                var message = string.Format("The key '{0}' was not found in the variable collection", variableName);
                var property = WellKnownVariables.AllVariables.SingleOrDefault(item => item.InvariantName.Equals(variableName, StringComparison.InvariantCultureIgnoreCase));

                if (property != null)
                {
                    message += string.Format(". (The variable is a wellknown property {0}.{1})",
                                             typeof (WellKnownVariables), property.WellknownName);
                }

                throw new BuildException(message, variables);
            }

            return variable;
        }
    }
}