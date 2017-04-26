using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Microsoft.Win32;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.MSBuild
{
    [UsedImplicitly]
    public class MSBuildVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
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

                return Task.FromResult<IEnumerable<IVariable>>(variables);
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
            return Task.FromResult<IEnumerable<IVariable>>(environmentVariables);
        }
    }
}
