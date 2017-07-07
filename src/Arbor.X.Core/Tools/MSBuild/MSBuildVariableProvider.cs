using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Microsoft.Win32;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.MSBuild
{
    [UsedImplicitly]
    public class MSBuildVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            int currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.WriteVerbose(
                $"Running current process [id {Process.GetCurrentProcess().Id}] as a {currentProcessBits}-bit process");

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

            string vsWherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio",
                "Installer",
                "vswhere.exe");

            if (File.Exists(vsWherePath))
            {
                logger.WriteDebug($"vswhere.exe exists at '{vsWherePath}'");

                ExitCode versionExitCode = await ProcessHelper.ExecuteAsync(
                    vsWherePath,
                    new List<string> { "-prerelease" },
                    cancellationToken: cancellationToken);

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

                await ProcessRunner.ExecuteAsync(
                    vsWherePath,
                    arguments: vsWhereArgs,
                    standardOutLog: (message, category) => resultBuilder.Append(message),
                    cancellationToken: cancellationToken,
                    toolAction: logger.WriteDebug,
                    standardErrorAction: logger.WriteError);

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
                            logger.Write($"Found MSBuild with vswhere.exe at '{msbuildPath}'");

                            var variables = new[]
                            {
                                new EnvironmentVariable(
                                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                                    msbuildPath)
                            };

                            return variables;
                        }
                    }

                    logger.Write($"Could not find any version of MSBuild.exe with vswhere.exe");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Could not deserialize installed Visual Studio versions from json '{json}'",
                        ex);
                }
            }

            var possiblePaths = new[]
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

            string fileBasedLookupResultPath = possiblePaths.FirstOrDefault(File.Exists);

            if (fileBasedLookupResultPath != null)
            {
                logger.Write($"Found MSBuild at '{fileBasedLookupResultPath}'");

                var variables = new[]
                {
                    new EnvironmentVariable(
                        WellKnownVariables.ExternalTools_MSBuild_ExePath,
                        fileBasedLookupResultPath)
                };

                return variables;
            }

            string foundPath = null;

            foreach (SemanticVersion possibleVersion in possibleVersions)
            {
                string registryKeyName = @"SOFTWARE\Microsoft\MSBuild\" + possibleVersion.Major + "." +
                                         possibleVersion.Minor;
                object msBuildPathRegistryKeyValue = null;
                const string valueKey = "MSBuildOverrideTasksPath";

                logger.WriteVerbose(
                    $"Looking for MSBuild exe path in {registryLookupBits}-bit registry key '{registryKeyName}\\{valueKey}");

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
                    logger.WriteVerbose(
                        $"Using MSBuild exe path '{foundPath}' defined in {registryLookupBits}-bit registry key {registryKeyName}\\{valueKey}");
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                const string msbuildPath = "MSBUILD_PATH";
                string fromEnvironmentVariable = Environment.GetEnvironmentVariable(msbuildPath);

                if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
                {
                    logger.Write($"Using MSBuild exe path '{foundPath}' from environment variable {msbuildPath}");
                    foundPath = fromEnvironmentVariable;
                }
                else
                {
                    logger.WriteError(
                        $"The MSBuild path could not be found in the {registryLookupBits}-bit registry keys.");
                    return null;
                }
            }

            logger.Write($"Using MSBuild exe path '{foundPath}'");

            var environmentVariables = new[]
            {
                new EnvironmentVariable(
                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                    foundPath)
            };
            return environmentVariables;
        }
    }
}
