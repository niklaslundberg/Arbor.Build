using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.BuildVariables
{
    public static class EnvironmentVariableHelper
    {
        public static IReadOnlyCollection<IVariable> GetBuildVariablesFromEnvironmentVariables()
        {
            var buildVariables = new List<IVariable>();

            var environmentVariables = Environment.GetEnvironmentVariables();

            var variables = environmentVariables
                .OfType<DictionaryEntry>()
                .Select(entry => new EnvironmentVariable(entry.Key.ToString(),
                    entry.Value.ToString()));

            buildVariables.AddRange(variables);

            return buildVariables;
        }
    }
}