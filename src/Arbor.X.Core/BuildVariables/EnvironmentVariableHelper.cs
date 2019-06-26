using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Arbor.Aesculus.Core;
using Arbor.Exceptions;
using Arbor.KVConfiguration.Schema.Json;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.BuildVariables
{
    public static class EnvironmentVariableHelper
    {
        public static IReadOnlyCollection<IVariable> GetBuildVariablesFromEnvironmentVariables(
            ILogger logger,
            List<IVariable> existingItems = null)
        {
            logger = logger ?? Logger.None;
            List<IVariable> existing = existingItems ?? new List<IVariable>();
            var buildVariables = new List<IVariable>();

            IDictionary environmentVariables = Environment.GetEnvironmentVariables();

            List<BuildVariable> variables = environmentVariables
                .OfType<DictionaryEntry>()
                .Select(entry => new BuildVariable(
                    entry.Key.ToString(),
                    entry.Value.ToString()))
                .ToList();

            List<BuildVariable> nonExisting = variables
                .Where(bv => !existing.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            List<BuildVariable> existingVariables = variables.Except(nonExisting).ToList();

            if (existingVariables.Count > 0)
            {
                var builder = new StringBuilder();

                builder.Append(
                        "There are ").Append(existingVariables)
                    .AppendLine(" existing variables that will not be overriden by environment variables:");

                foreach (BuildVariable environmentVariable in existingVariables)
                {
                    builder.Append(environmentVariable.Key).Append(": ").AppendLine(environmentVariable.Value);
                }

                logger.Verbose("{Variables}", builder.ToString());
            }

            buildVariables.AddRange(nonExisting);

            return buildVariables;
        }

        public static ImmutableArray<KeyValue> GetBuildVariablesFromFile([NotNull] ILogger logger, string fileName)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            string currentDirectory = VcsPathHelper.FindVcsRootPath();

            if (currentDirectory == null)
            {
                logger.Error("Could not find source root");
                return ImmutableArray<KeyValue>.Empty;
            }

            var fileInfo = new FileInfo(Path.Combine(currentDirectory, fileName));

            if (!fileInfo.Exists)
            {
                logger.Warning(
                    "The environment variable file '{FileInfo}' does not exist, skipping setting environment variables from file '{FileName}'",
                    fileInfo,
                    fileName);
                return ImmutableArray<KeyValue>.Empty;
            }

            ConfigurationItems configurationItems;

            try
            {
                configurationItems = new KVConfiguration.JsonConfiguration.JsonFileReader(fileInfo.FullName)
                    .GetConfigurationItems();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not parse key value pairs in file '{FullName}'", fileInfo.FullName);

                return ImmutableArray<KeyValue>.Empty;
            }

            if (configurationItems == null)
            {
                logger.Error("Could not parse key value pairs in file '{FullName}'", fileInfo.FullName);
                return ImmutableArray<KeyValue>.Empty;
            }

            logger.Information("Used configuration values from file '{FileName}'", fileName);

            return configurationItems.Keys;
        }
    }
}
