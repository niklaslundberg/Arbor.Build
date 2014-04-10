using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.BuildVariables
{
    public static class BuildVariableExtensions
    {
        public static bool HasKey(this IReadOnlyCollection<IVariable> buildVariables, string key)
        {
            return buildVariables.Any(
                bv => bv.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static IVariable GetVariable(this IReadOnlyCollection<IVariable> buildVariables, string key)
        {
            return buildVariables.Single(
                bv => bv.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}