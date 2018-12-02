using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Processing;
using Arbor.Processing;
using JetBrains.Annotations;
using Microsoft.Win32;
using Newtonsoft.Json;
using NuGet.Versioning;
using Serilog;

namespace Arbor.Build.Core.Tools.MSBuild
{
    [UsedImplicitly]
    public class MSBuildVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            int currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.Verbose("Running current process [id {Id}] as a {CurrentProcessBits}-bit process",
                Process.GetCurrentProcess().Id,
                currentProcessBits);

            List<SemanticVersion> possibleVersions = new List<string> { "15.0.0", "14.0.0", "12.0.0", "4.0.0" }
                .Select(SemanticVersion.Parse)
                .ToList();

            string max = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_MSBuild_MaxVersion,
                "15.0.0");

            SemanticVersion[] toRemove = possibleVersions.Where(version => version > SemanticVersion.Parse(max))
                .ToArray();

            foreach (SemanticVersion semVersion in toRemove)
            {
                possibleVersions.Remove(semVersion);
            }

            string vsWherePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio",
                "Installer",
                "vswhere.exe");

            if (File.Exists(vsWherePath))
            {
                logger.Debug("vswhere.exe exists at '{VsWherePath}'", vsWherePath);

                ExitCode versionExitCode = await ProcessHelper.ExecuteAsync(
                    vsWherePath,
                    new List<string> { "-prerelease" },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var vsWhereArgs = new List<string> { "-requires", "Microsoft.Component.MSBuild", "-format", "json" };

                bool allowPreRelease =
                    buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_AllowPrereleaseEnabled);

                if (allowPreRelease)
                {
                    // NOTE only newer releases of vswhere.exe supports -prerelease flag
                    if (versionExitCode.IsSuccess)
                    {
                        vsWhereArgs.Add("-prerelease");
                    }
                }

                var resultBuilder = new StringBuilder();

                ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(
                    vsWherePath,
                    arguments: vsWhereArgs,
                    standardOutLog: (message, category) => resultBuilder.Append(message),
                    cancellationToken: cancellationToken,
                    toolAction: logger.Debug,
                    standardErrorAction: logger.Error).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    throw new InvalidOperationException("Could not get Visual Studio path");
                }

                string json = resultBuilder.ToString();

                try
                {
                    var instanceTypePattern = new[]
                    {
                        new
                        {
                            installationName = string.Empty,
                            installationPath = string.Empty,
                            installationVersion = string.Empty,
                            channelId = string.Empty
                        }
                    };

                    var typedInstallations = JsonConvert.DeserializeAnonymousType(
                        json,
                        instanceTypePattern);

                    var candidates = typedInstallations.ToArray();

                    if (!allowPreRelease)
                    {
                        candidates = candidates
                            .Where(candidate =>
                                candidate.channelId.IndexOf("preview", StringComparison.OrdinalIgnoreCase) < 0)
                            .ToArray();
                    }

                    var array = candidates
                        .Select(candidate => new { candidate, versoin = Version.Parse(candidate.installationVersion) })
                        .ToArray();

                    var firstOrDefault = array.OrderByDescending(candidateItem => candidateItem.versoin)
                        .FirstOrDefault();

                    if (firstOrDefault != null)
                    {
                        string msbuildPath = Path.Combine(
                            firstOrDefault.candidate.installationPath,
                            "MSBuild",
                            "15.0",
                            "bin",
                            "MSBuild.exe");

                        if (File.Exists(msbuildPath))
                        {
                            logger.Information("Found MSBuild with vswhere.exe at '{MsbuildPath}'", msbuildPath);

                            var variables = new IVariable[]
                            {
                                new BuildVariable(
                                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                                    msbuildPath)
                            };

                            return variables.ToImmutableArray();
                        }
                    }

                    logger.Information("Could not find any version of MSBuild.exe with vswhere.exe");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Could not deserialize installed Visual Studio versions from json '{json}'",
                        ex);
                }
            }

            string[] possiblePaths = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "Enterprise",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "Profesional",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "Community",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "BuildTools",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe")
            };

            string fileBasedLookupResultPath = Array.Find(possiblePaths, File.Exists);

            if (fileBasedLookupResultPath != null)
            {
                logger.Information("Found MSBuild at '{FileBasedLookupResultPath}'", fileBasedLookupResultPath);

                var variables = new IVariable[]
                {
                    new BuildVariable(
                        WellKnownVariables.ExternalTools_MSBuild_ExePath,
                        fileBasedLookupResultPath)
                };

                return variables.ToImmutableArray();
            }

            string foundPath = null;

            foreach (SemanticVersion possibleVersion in possibleVersions)
            {
                string registryKeyName = @"SOFTWARE\Microsoft\MSBuild\" + possibleVersion.Major + "." +
                                         possibleVersion.Minor;
                object msBuildPathRegistryKeyValue = null;
                const string valueKey = "MSBuildOverrideTasksPath";

                logger.Verbose(
                    "Looking for MSBuild exe path in {RegistryLookupBits}-bit registry key '{RegistryKeyName}\\{ValueKey}",
                    registryLookupBits,
                    registryKeyName,
                    valueKey);

                using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey key = view32.OpenSubKey(registryKeyName))
                    {
                        if (key != null)
                        {
                            msBuildPathRegistryKeyValue = key.GetValue(valueKey, null);
                        }
                    }
                }

                string msBuildPath = msBuildPathRegistryKeyValue != null
                    ? $"{msBuildPathRegistryKeyValue}MSBuild.exe"
                    : null;

                if (!string.IsNullOrWhiteSpace(msBuildPath))
                {
                    foundPath = msBuildPath;
                    logger.Verbose(
                        "Using MSBuild exe path '{FoundPath}' defined in {RegistryLookupBits}-bit registry key {RegistryKeyName}\\{ValueKey}",
                        foundPath,
                        registryLookupBits,
                        registryKeyName,
                        valueKey);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                const string msbuildPath = "MSBUILD_PATH";
                string fromEnvironmentVariable = Environment.GetEnvironmentVariable(msbuildPath);

                if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
                {
                    logger.Information("Using MSBuild exe path '{FoundPath}' from environment variable {MsbuildPath}",
                        foundPath,
                        msbuildPath);
                    foundPath = fromEnvironmentVariable;
                }
                else
                {
                    logger.Error("The MSBuild path could not be found in the {RegistryLookupBits}-bit registry keys.",
                        registryLookupBits);
                    return ImmutableArray<IVariable>.Empty;
                }
            }

            logger.Information("Using MSBuild exe path '{FoundPath}'", foundPath);

            var environmentVariables = new IVariable[]
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                    foundPath)
            };
            return environmentVariables.ToImmutableArray();
        }
    }
}
