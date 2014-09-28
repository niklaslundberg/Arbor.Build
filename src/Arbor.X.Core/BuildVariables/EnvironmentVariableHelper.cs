using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.BuildVariables
{
    public static class EnvironmentVariableHelper
    {
        public static IReadOnlyCollection<IVariable> GetBuildVariablesFromEnvironmentVariables(ILogger logger, List<IVariable> existingItems = null)
        {
            var existing = existingItems ?? new List<IVariable>();
            var buildVariables = new List<IVariable>();

            var environmentVariables = Environment.GetEnvironmentVariables();

            var variables = environmentVariables
                .OfType<DictionaryEntry>()
                .Select(entry => new EnvironmentVariable(entry.Key.ToString(),
                    entry.Value.ToString()))
                .ToList();

            var nonExisting = variables
                .Where(bv => existing.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            var existingVariables = variables.Except(nonExisting).ToList();

            if (existingVariables.Any())
            {
                var builder = new StringBuilder();

                builder.AppendLine(string.Format("There are {0} existing variables that will not be overriden by environment variables:", existingVariables));

                foreach (var environmentVariable in existingVariables)
                {
                    builder.AppendLine(environmentVariable.Key + ": " + environmentVariable.Value);
                }
                logger.WriteVerbose(builder.ToString());
            }

            buildVariables.AddRange(nonExisting);

            return buildVariables;
        }
    }
}