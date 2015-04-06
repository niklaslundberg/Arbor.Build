﻿using System;
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

namespace Arbor.X.Core.Tools.MSBuild
{
    public class MSBuildVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.WriteVerbose(string.Format("Running current process [id {0}] as a {1}-bit process",
                Process.GetCurrentProcess().Id, currentProcessBits));

            var possibleVersions = new List<string> {"14.0", "12.0", "4.0"}.Select(version => SemVersion.Parse(version)).ToList();

            var max = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_MaxVersion,
                "14.0");

            var toRemove = possibleVersions.Where(version => version > SemVersion.Parse(max));

            foreach (var semVersion in toRemove)
            {
                possibleVersions.Remove(semVersion);
            }

            string foundPath = null;

            foreach (var possibleVersion in possibleVersions)
            {
                string registryKeyName = @"SOFTWARE\Microsoft\MSBuild\" + possibleVersion.Major + "." + possibleVersion.Minor;
                object msBuildPathRegistryKeyValue = null;
                const string valueKey = "MSBuildOverrideTasksPath";

                logger.WriteVerbose(string.Format("Looking for MSBuild exe path in {0}-bit registry key '{1}\\{2}",
                    registryLookupBits,
                    registryKeyName, valueKey));

                using (var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = view32.OpenSubKey(registryKeyName))
                    {
                        if (key != null)
                        {
                            msBuildPathRegistryKeyValue = key.GetValue(valueKey, null);
                        }
                    }
                }

                var msBuildPath = msBuildPathRegistryKeyValue != null
                    ? string.Format("{0}MSBuild.exe", msBuildPathRegistryKeyValue)
                    : null;

                if (!string.IsNullOrWhiteSpace(msBuildPath))
                {
                    foundPath = msBuildPath;
                    logger.WriteVerbose(string.Format("Using MSBuild exe path '{0}' defined in {1}-bit registry key {2}\\{3}",
                        foundPath, registryLookupBits, registryKeyName, valueKey));
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                const string msbuildPath = "MSBUILD_PATH";
                var fromEnvironmentVariable = Environment.GetEnvironmentVariable(msbuildPath);

                if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
                {
                    logger.Write(string.Format("Using MSBuild exe path '{0}' from environment variable {1}", foundPath,
                        msbuildPath));
                    foundPath = fromEnvironmentVariable;
                }
                else
                {
                    logger.WriteError(string.Format("The MSBuild path could not be found in the {0}-bit registry keys.",
                        registryLookupBits));
                    return null;
                }
            }

            logger.Write(string.Format("Using MSBuild exe path '{0}'", foundPath));

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