using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Microsoft.Win32;

namespace Arbor.X.Core.Tools.MSBuild
{
    public class MSBuildVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.Write(string.Format("Running current process [id {0}] as a {1}-bit process",
                Process.GetCurrentProcess().Id, currentProcessBits));

            var possibleVersions = new List<string> {"12.0", "4.0"};

            string foundPath = null;

            foreach (var possibleVersion in possibleVersions)
            {
                string registryKeyName = @"SOFTWARE\Microsoft\MSBuild\" + possibleVersion;
                object msBuildPathRegistryKeyValue = null;
                const string valueKey = "MSBuildOverrideTasksPath";

                logger.Write(string.Format("Looking for MSBuild exe path in {0}-bit registry key '{1}\\{2}",
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
                    logger.Write(string.Format("Using MSBuild exe path '{0}' defined in {1}-bit registry key {2}\\{3}",
                        foundPath, registryLookupBits, registryKeyName, valueKey));
                    foundPath = msBuildPath;
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
    }
}