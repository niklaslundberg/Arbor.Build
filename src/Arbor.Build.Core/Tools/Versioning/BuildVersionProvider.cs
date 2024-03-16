using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Int;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Exceptions;
using Arbor.FS;
using Arbor.KVConfiguration.Core.Extensions.StringExtensions;
using Arbor.KVConfiguration.Core.Metadata;
using Arbor.KVConfiguration.JsonConfiguration;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Versioning;

[UsedImplicitly]
public class BuildVersionProvider(ITimeService timeService, BuildContext buildContext) : IVariableProvider
{
    public int Order => VariableProviderOrder.Ignored;

    public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        IEnumerable<KeyValuePair<string, string>> variables =
            GetVersionVariables(buildVariables, logger);

        var environmentVariables = variables
            .Select(item => (IVariable)new BuildVariable(item.Key, item.Value))
            .ToList();

        return Task.FromResult(environmentVariables.ToImmutableArray());
    }

    private static bool ValidateVersionNumber(KeyValuePair<string, string?> pair)
    {
        if (string.IsNullOrWhiteSpace(pair.Value))
        {
            return false;
        }

        if (!int.TryParse(pair.Value, out int parsed) || parsed < 0)
        {
            return false;
        }

        return true;
    }

    private IEnumerable<KeyValuePair<string, string>> GetVersionVariables(
        IReadOnlyCollection<IVariable> buildVariables,
        ILogger logger)
    {
        var environmentVariables =
            buildVariables.Select(item => new KeyValuePair<string, string?>(item.Key, item.Value)).ToList();

        int major = -1;
        int minor = -1;
        int patch = -1;
        int build = -1;

        const string fileName = "version.json";

        var sourceRoot = buildContext.SourceRoot;

        var versionFileName = UPath.Combine(sourceRoot.Path, fileName);

        if (sourceRoot.FileSystem.FileExists(versionFileName))
        {
            logger.Verbose("A version file was found with name {VersionFileName} at source root '{SourceRoot}'",
                versionFileName,
                sourceRoot);
            IReadOnlyCollection<KeyValueConfigurationItem>? keyValueConfigurationItems = null;
            try
            {
                keyValueConfigurationItems =
                    new JsonFileReader(buildContext.SourceRoot.FileSystem.ConvertPathToInternal(versionFileName)).ReadConfiguration();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Warning("Could not read the configuration content in file '{VersionFileName}'",
                    versionFileName);
            }

            if (keyValueConfigurationItems != null)
            {
                var jsonKeyValueConfiguration = new JsonKeyValueConfiguration(keyValueConfigurationItems);

                const string majorKey = "major";

                const string minorKey = "minor";

                const string patchKey = "patch";

                var required = new Dictionary<string, string?>
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
                    major = required[majorKey].ParseOrDefault(-1);
                    minor = required[minorKey].ParseOrDefault(-1);
                    patch = required[patchKey].ParseOrDefault(-1);

                    logger.Verbose(
                        "All version numbers from the version file '{VersionFileName}' were parsed successfully",
                        versionFileName);
                }
                else
                {
                    logger.Verbose(
                        "Not all version numbers from the version file '{VersionFileName}' were parsed successfully",
                        versionFileName);
                }
            }
        }
        else
        {
            logger.Verbose(
                "No version file found with name {VersionFileName} at source root '{SourceRoot}' was found",
                sourceRoot.FileSystem.ConvertPathToInternal(versionFileName),
                sourceRoot.ConvertPathToInternal());
        }

        int envMajor = environmentVariables.IntValueOrDefault(WellKnownVariables.VersionMajor, -1);
        int envMinor = environmentVariables.IntValueOrDefault(WellKnownVariables.VersionMinor, -1);
        int envPatch = environmentVariables.IntValueOrDefault(WellKnownVariables.VersionPatch, -1);
        int envBuild = environmentVariables.IntValueOrDefault(WellKnownVariables.VersionBuild, -1);

        int? teamCityBuildVersion =
            environmentVariables.Where(item => item.Key == WellKnownVariables.TeamCityVersionBuild)
                .Select(item =>
                {
                    if (int.TryParse(item.Value, out int buildVersion) && buildVersion >= 0)
                    {
                        return (int?)buildVersion;
                    }

                    return (int?)-1;
                })
                .SingleOrDefault();

        if (envMajor >= 0)
        {
            logger.Verbose("Found major {EnvMajor} version in build variable", envMajor);
            major = envMajor;
        }

        if (envMinor >= 0)
        {
            logger.Verbose("Found minor {EnvMinor} version in build variable", envMinor);
            minor = envMinor;
        }

        if (envPatch >= 0)
        {
            logger.Verbose("Found patch {EnvPatch} version in build variable", envPatch);
            patch = envPatch;
        }

        if (envBuild >= 0)
        {
            logger.Verbose("Found build {EnvBuild} version in build variable", envBuild);
            build = envBuild;
        }

        if (major < 0)
        {
            logger.Verbose("Found no major version, using version 0");
            major = 0;
        }

        if (minor < 0)
        {
            logger.Verbose("Found no minor version, using version 0");
            minor = 0;
        }

        if (patch < 0)
        {
            logger.Verbose("Found no patch version, using version 0");
            patch = 0;
        }

        if (build < 0)
        {
            if (teamCityBuildVersion.HasValue)
            {
                build = teamCityBuildVersion.Value;
                logger.Verbose(
                    "Found no build version, using version {Build} from TeamCity ({TeamCityVersionBuild})",
                    build,
                    WellKnownVariables.TeamCityVersionBuild);
            }
            else if (buildVariables.GetBooleanByKey(WellKnownVariables.BuildNumberAsUnixEpochSecondsEnabled, defaultValue: true))
            {
                build = (int) timeService.UtcNow().ToUnixTimeSeconds();
            }
            else
            {
                logger.Verbose("Found no build version, using version 0");
                build = 0;
            }
        }

        if (major == 0 && minor == 0 && patch == 0)
        {
            logger.Verbose("Major minor and build versions are all 0, setting minor version to 1");
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