using System;
using System.Collections.Generic;
using System.Linq;

using Arbor.X.Core.Exceptions;

using FubuCore.Logging;

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

            var foundVariables = variables.Where(@var => @var.Key.Equals(variableName, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (foundVariables.Count() > 1)
            {
                throw new InvalidOperationException(string.Format("The are multiple variables with key '{0}'", variableName));
            }

            var variable = foundVariables.SingleOrDefault();

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