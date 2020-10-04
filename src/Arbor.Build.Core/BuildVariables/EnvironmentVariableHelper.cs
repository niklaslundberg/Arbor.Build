using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Arbor.Exceptions;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.Schema.Json;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Zio;

namespace Arbor.Build.Core.BuildVariables
{
    public static class EnvironmentVariableHelper
    {
        public static IReadOnlyCollection<IVariable> GetBuildVariablesFromEnvironmentVariables(
            ILogger logger,
            IEnvironmentVariables environmentVariables,
            List<IVariable>? existingItems = null)
        {
            logger ??= Logger.None ?? throw new ArgumentNullException(nameof(logger));
            List<IVariable> existing = existingItems ?? new List<IVariable>();
            var buildVariables = new List<IVariable>();

            var variables = environmentVariables.GetVariables()
                .Where(pair => pair.Value is {})
                .Select(pair => new BuildVariable(pair.Key, pair.Value));

            var nonExisting = variables
                .Where(bv => !existing.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var existingVariables = variables
                .Where(bv => nonExisting.Any(ebv => ebv.Key.Equals(bv.Key, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(item => item.Key)
                .ToList();

            if (existingVariables.Count > 0)
            {
                var builder = new StringBuilder();

                builder.Append(
                        "There are ").Append(string.Join(", ", existingVariables))
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

        public static ImmutableArray<KeyValue> GetBuildVariablesFromFile([NotNull] ILogger logger,
            string fileName,
            DirectoryEntry sourceRoot)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var file = new FileEntry(sourceRoot.FileSystem, sourceRoot.Path / fileName);

            if (!file.Exists)
            {
                logger.Debug(
                    "The environment variable file '{File}' does not exist, skipping setting environment variables from file '{FileName}'",
                    file,
                    fileName);

                return ImmutableArray<KeyValue>.Empty;
            }

            ConfigurationItems configurationItems;

            try
            {
                configurationItems = new JsonFileReader(sourceRoot.FileSystem.ConvertPathToInternal(file.FullName))
                    .GetConfigurationItems();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not parse key value pairs in file '{FullName}'", file.FullName);

                return ImmutableArray<KeyValue>.Empty;
            }

            if (configurationItems == null)
            {
                logger.Error("Could not parse key value pairs in file '{FullName}'", file.FullName);
                return ImmutableArray<KeyValue>.Empty;
            }

            logger.Information("Used configuration values from file '{FileName}'", fileName);

            return configurationItems.Keys;
        }
    }
}