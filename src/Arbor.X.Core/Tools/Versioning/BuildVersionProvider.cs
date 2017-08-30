using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Exceptions;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Core.Extensions.StringExtensions;
using Arbor.KVConfiguration.Core.Metadata;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.Schema;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class BuildVersionProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, string>> variables =
                GetVersionVariables(buildVariables, logger);

            List<EnvironmentVariable> environmentVariables = variables
                .Select(item => new EnvironmentVariable(item.Key, item.Value))
                .ToList();

            return Task.FromResult<IEnumerable<IVariable>>(environmentVariables);
        }

        private static bool ValidateVersionNumber(KeyValuePair<string, string> s)
        {
            if (string.IsNullOrWhiteSpace(s.Value))
            {
                return false;
            }

            int parsed;

            if (!int.TryParse(s.Value, out parsed) || parsed < 0)
            {
                return false;
            }

            return true;
        }

        private IEnumerable<KeyValuePair<string, string>> GetVersionVariables(
            IReadOnlyCollection<IVariable> buildVariables,
            ILogger logger)
        {
            List<KeyValuePair<string, string>> environmentVariables =
                buildVariables.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList();

            int major = -1;
            int minor = -1;
            int patch = -1;
            int build = -1;

            string sourceRoot = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string fileName = "version.json";

            string versionFileName = Path.Combine(sourceRoot, fileName);

            if (File.Exists(versionFileName))
            {
                logger.WriteVerbose(
                    $"A version file was found with name {versionFileName} at source root '{sourceRoot}'");
                IReadOnlyCollection<KeyValueConfigurationItem> keyValueConfigurationItems = null;
                try
                {
                    keyValueConfigurationItems =
                        new JsonFileReader(versionFileName).ReadConfiguration();
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.WriteWarning($"Could not read the configuration content in file '{versionFileName}'");
                }

                if (keyValueConfigurationItems != null)
                {
                    var jsonKeyValueConfiguration = new JsonKeyValueConfiguration(keyValueConfigurationItems);

                    string majorKey = "major"; // TODO defined major key

                    string minorKey = "minor"; // TODO defined minor key

                    string patchKey = "patch"; // TODO defined patch key

                    var required = new Dictionary<string, string>
                    {
                        {
                            majorKey,
                            jsonKeyValueConfiguration.ValueOrDefault(
                                majorKey)
                        },
                        {
                            minorKey,
                            jsonKeyValueConfiguration.ValueOrDefault(
                                minorKey)
                        },
                        {
                            patchKey,
                            jsonKeyValueConfiguration.ValueOrDefault(
                                patchKey)
                        }
                    };

                    if (required.All(ValidateVersionNumber))
                    {
                        major = required[majorKey].TryParseInt32().Value;
                        minor = required[minorKey].TryParseInt32().Value;
                        patch = required[patchKey].TryParseInt32().Value;

                        logger.WriteVerbose(
                            $"All version numbers from the version file '{versionFileName}' were parsed successfully");
                    }
                    else
                    {
                        logger.WriteVerbose(
                            $"Not all version numbers from the version file '{versionFileName}' were parsed successfully");
                    }
                }
            }
            else
            {
                logger.WriteVerbose(
                    $"No version file found with name {versionFileName} at source root '{sourceRoot}' was found");
            }

            int envMajor =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMajor)
                    .Select(item => (int?)int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int envMinor =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMinor)
                    .Select(item => (int?)int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int envPatch =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionPatch)
                    .Select(item => (int?)int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int envBuild =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionBuild)
                    .Select(item => (int?)int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;

            int teamCityBuildVersion =
                environmentVariables.Where(item => item.Key == WellKnownVariables.TeamCity.TeamCityVersionBuild)
                    .Select(item =>
                    {
                        if (int.TryParse(item.Value, out int buildVersion) && buildVersion >= 0)
                        {
                            return buildVersion;
                        }

                        return -1;
                    })
                    .SingleOrDefault();

            if (envMajor >= 0)
            {
                logger.WriteVerbose($"Found major {envMajor} version in build variable");
                major = envMajor;
            }

            if (envMinor >= 0)
            {
                logger.WriteVerbose($"Found minor {envMinor} version in build variable");
                minor = envMinor;
            }

            if (envPatch >= 0)
            {
                logger.WriteVerbose($"Found patch {envPatch} version in build variable");
                patch = envPatch;
            }

            if (envBuild >= 0)
            {
                logger.WriteVerbose($"Found build {envBuild} version in build variable");
                build = envBuild;
            }

            if (major < 0)
            {
                logger.WriteVerbose($"Found no major version, using version 0");
                major = 0;
            }

            if (minor < 0)
            {
                logger.WriteVerbose("Found no minor version, using version 0");
                minor = 0;
            }

            if (patch < 0)
            {
                logger.WriteVerbose("Found no patch version, using version 0");
                patch = 0;
            }

            if (build < 0)
            {
                if (teamCityBuildVersion >= 0)
                {
                    build = teamCityBuildVersion;
                    logger.WriteVerbose($"Found no build version, using version {build} from TeamCity ({WellKnownVariables.TeamCity.TeamCityVersionBuild})");
                }
                else
                {
                    logger.WriteVerbose("Found no build version, using version 0");
                    build = 0;
                }
            }

            if (major == 0 && minor == 0 && patch == 0)
            {
                logger.WriteVerbose("Major minor and build versions are all 0, setting minor version to 1");
                minor = 1;
            }

            var netAssemblyVersion = new Version(major, minor, 0, 0);
            var fullVersion = new Version(major, minor, patch, build);
            string fullVersionValue = fullVersion.ToString(4);
            var netAssemblyFileVersion = new Version(major, minor, patch, build);

            yield return
                new KeyValuePair<string, string>(WellKnownVariables.NetAssemblyVersion, netAssemblyVersion.ToString(4));
            yield return
                new KeyValuePair<string, string>(
                    WellKnownVariables.NetAssemblyFileVersion,
                    netAssemblyFileVersion.ToString(4));
            yield return new KeyValuePair<string, string>(
                WellKnownVariables.VersionMajor,
                major.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(
                WellKnownVariables.VersionMinor,
                minor.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(
                WellKnownVariables.VersionPatch,
                patch.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(
                WellKnownVariables.VersionBuild,
                build.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>("Version", fullVersionValue);
            yield return new KeyValuePair<string, string>(WellKnownVariables.Version, fullVersionValue);
        }
    }
}
