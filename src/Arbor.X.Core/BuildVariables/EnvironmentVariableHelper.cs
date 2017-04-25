using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arbor.Aesculus.Core;
using Arbor.Exceptions;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.Processing.Core;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.BuildVariables
{
    public static class EnvironmentVariableHelper
    {
        public static IReadOnlyCollection<IVariable> GetBuildVariablesFromEnvironmentVariables(ILogger logger,
            List<IVariable> existingItems = null)
        {
            List<IVariable> existing = existingItems ?? new List<IVariable>();
            var buildVariables = new List<IVariable>();

            IDictionary environmentVariables = Environment.GetEnvironmentVariables();

            List<EnvironmentVariable> variables = environmentVariables
                .OfType<DictionaryEntry>()
                .Select(entry => new EnvironmentVariable(entry.Key.ToString(),
                    entry.Value.ToString()))
                .ToList();

            List<EnvironmentVariable> nonExisting = variables
                .Where(bv => !existing.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            List<EnvironmentVariable> existingVariables = variables.Except(nonExisting).ToList();

            if (existingVariables.Any())
            {
                var builder = new StringBuilder();

                builder.AppendLine(
                    $"There are {existingVariables} existing variables that will not be overriden by environment variables:");

                foreach (EnvironmentVariable environmentVariable in existingVariables)
                {
                    builder.AppendLine(environmentVariable.Key + ": " + environmentVariable.Value);
                }
                logger.WriteVerbose(builder.ToString());
            }

            buildVariables.AddRange(nonExisting);

            return buildVariables;
        }

        public static ExitCode SetEnvironmentVariablesFromFile([NotNull] ILogger logger, string fileName)
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

            var fileInfo = new FileInfo(Path.Combine(currentDirectory, fileName));

            if (!fileInfo.Exists)
            {
                logger.WriteWarning(
                    $"The environment variable file '{fileInfo}' does not exist, skipping setting environment variables from file '{fileName}'");
                return ExitCode.Success;
            }

            ConfigurationItems configurationItems;

            try
            {
                configurationItems = new KVConfiguration.JsonConfiguration.JsonFileReader(fileInfo.FullName)
                    .GetConfigurationItems();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.WriteError($"Could not parse key value pairs in file '{fileInfo.FullName}', {ex}");
                return ExitCode.Failure;
            }

            if (configurationItems == null)
            {
                logger.WriteError($"Could not parse key value pairs in file '{fileInfo.FullName}'");
                return ExitCode.Failure;
            }

            foreach (KeyValue keyValuePair in configurationItems.Keys)
            {
                try
                {
                    Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
                    logger.WriteDebug(
                        $"Set environment variable with key '{keyValuePair.Key}' and value '{keyValuePair.Value}' from file '{fileName}'");
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.WriteError(
                        $"Could not set environment variable with key '{keyValuePair.Key}' and value '{keyValuePair.Value}' from file '{fileName}'");
                    return ExitCode.Failure;
                }
            }

            logger.Write($"Used configuration values from file '{fileName}'");

            return ExitCode.Success;
        }
    }
}
