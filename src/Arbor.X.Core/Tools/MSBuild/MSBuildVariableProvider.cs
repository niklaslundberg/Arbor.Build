using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;
using Microsoft.Win32;
using Semver;
using System.Linq;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.MSBuild
{
    [UsedImplicitly]
    public class MSBuildVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int RegistryLookupBits = 32;
            logger.WriteVerbose(
                $"Running current process [id {Process.GetCurrentProcess().Id}] as a {currentProcessBits}-bit process");

            var possibleVersions = new List<string> {"14.0", "12.0", "4.0"}.Select(version => SemVersion.Parse(version)).ToList();

            var max = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_MaxVersion,
                "14.0");

            var toRemove = possibleVersions.Where(version => version > SemVersion.Parse(max)).ToArray();

            foreach (var semVersion in toRemove)
            {
                possibleVersions.Remove(semVersion);
            }

            string foundPath = null;

            foreach (var possibleVersion in possibleVersions)
            {
                string registryKeyName = @"SOFTWARE\Microsoft\MSBuild\" + possibleVersion.Major + "." + possibleVersion.Minor;
                object msBuildPathRegistryKeyValue = null;
                const string ValueKey = "MSBuildOverrideTasksPath";

                logger.WriteVerbose(
                    $"Looking for MSBuild exe path in {RegistryLookupBits}-bit registry key '{registryKeyName}\\{ValueKey}");

                using (var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = view32.OpenSubKey(registryKeyName))
                    {
                        if (key != null)
                        {
                            msBuildPathRegistryKeyValue = key.GetValue(ValueKey, null);
                        }
                    }
                }

                var msBuildPath = msBuildPathRegistryKeyValue != null
                    ? $"{msBuildPathRegistryKeyValue}MSBuild.exe"
                                      : null;

                if (!string.IsNullOrWhiteSpace(msBuildPath))
                {
                    foundPath = msBuildPath;
                    logger.WriteVerbose(
                        $"Using MSBuild exe path '{foundPath}' defined in {RegistryLookupBits}-bit registry key {registryKeyName}\\{ValueKey}");
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                const string MsbuildPath = "MSBUILD_PATH";
                var fromEnvironmentVariable = Environment.GetEnvironmentVariable(MsbuildPath);

                if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
                {
                    logger.Write($"Using MSBuild exe path '{foundPath}' from environment variable {MsbuildPath}");
                    foundPath = fromEnvironmentVariable;
                }
                else
                {
                    logger.WriteError(
                        $"The MSBuild path could not be found in the {RegistryLookupBits}-bit registry keys.");
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

        public int Order => VariableProviderOrder.Ignored;
    }
}
