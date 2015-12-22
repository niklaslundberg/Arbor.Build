using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alphaleonis.Win32.Filesystem;

using Arbor.Aesculus.Core;
using Arbor.Castanea;
using Arbor.Processing.Core;
using Arbor.X.Core.Logging;

using JetBrains.Annotations;

using Newtonsoft.Json;

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
                .Where(bv => !existing.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.InvariantCultureIgnoreCase)))
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

        public static ExitCode SetEnvironmentVariablesFromFile([NotNull] ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            string currentDirectory = VcsPathHelper.FindVcsRootPath();

            if (currentDirectory == null)
            {
                logger.WriteError("Could not find source root");
                return ExitCode.Failure;
            }

            var fileInfo = new FileInfo(Path.Combine(currentDirectory, "arborx_environmentvariables.json"));

            if (!fileInfo.Exists)
            {
                logger.WriteWarning(
                    $"The environment variable file '{fileInfo}' does not exist, skipping setting environment variables from file");
                return ExitCode.Success;
            }

            var fileContent = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);

            KeyValuePair<string, string>[] pairs;

            try
            {
                pairs = JsonConvert.DeserializeObject<KeyValuePair<string, string>[]>(fileContent);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.WriteError($"Could not parse key value pairs in file '{fileInfo.FullName}'{ex}");
                return ExitCode.Failure;
            }

            foreach (var keyValuePair in pairs)
            {
                try
                {
                    Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
                    logger.WriteDebug($"Set environment variable with key '{keyValuePair.Key}' and value '{keyValuePair.Value}')");
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.WriteError($"Could not set environment variable with key '{keyValuePair.Key}' and value '{keyValuePair.Value}')");
                    return ExitCode.Failure;
                }
            }

            return ExitCode.Success;
        }
    }
}