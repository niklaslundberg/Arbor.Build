using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Microsoft.Win32;

namespace Arbor.X.Core.Tools.VisualStudio
{
    public class VisualStudioVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.Write(string.Format("Running current process [id {0}] as a {1}-bit process",
                Process.GetCurrentProcess().Id, currentProcessBits));

            const string registryKeyName = @"SOFTWARE\Microsoft\VisualStudio";

            logger.Write(string.Format(@"Looking for Visual Studio versions in {0}-bit registry key 'HKEY_LOCAL_MACHINE\{1}'", registryLookupBits,
                registryKeyName));

            var visualStudioVersion = GetVisualStudioVersion(logger, registryKeyName);

            string vsTestExePath = null;

            if (!string.IsNullOrWhiteSpace(visualStudioVersion))
            {
                logger.Write(string.Format("Found Visual Studio version {0}", visualStudioVersion));

                vsTestExePath = GetVSTestExePath(logger, registryKeyName, visualStudioVersion);
            }
            else
            {
                logger.WriteWarning("Could not find any Visual Studio version");
            }


            var environmentVariables = new[]
                                       {
                                           new EnvironmentVariable(
                                               WellKnownVariables.ExternalTools_VisualStudio_Version,
                                               visualStudioVersion),
                                               new EnvironmentVariable(WellKnownVariables.ExternalTools_VSTest_ExePath, vsTestExePath) 
                                       };

            return Task.FromResult<IEnumerable<IVariable>>(environmentVariables);
        }

        static string GetVisualStudioVersion(ILogger logger, string registryKeyName)
        {
            string visualStudioVersion = null;

            using (var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var vsKey = view32.OpenSubKey(registryKeyName))
                {
                    if (vsKey != null)
                    {
                        List<Version> names = vsKey.GetSubKeyNames()
                                                   .Where(name => char.IsDigit(name.First()))
                                                   .Select(name =>
                                                       {
                                                           var verison = Version.Parse(name);
                                                           return verison;
                                                       })
                                                   .OrderByDescending(name => name)
                                                   .ToList();

                        logger.WriteWarning(string.Format("Found {0} Visual Studio versions: {1}", names.Count,
                                                          string.Join(", ", names.Select(name => name.ToString(2)))));

                        if (names.Any(name => name == new Version(12, 0)))
                        {
                            visualStudioVersion = "12.0";
                        }
                        else if (names.Any(name => name == new Version(11, 0)))
                        {
                            visualStudioVersion = "11.0";
                        }
                        else if (names.Any())
                        {
                            visualStudioVersion = names.First().ToString(fieldCount: 2);
                        }
                    }
                }
            }
            return visualStudioVersion;
        }
        static string GetVSTestExePath(ILogger logger, string registryKeyName, string visualStudioVersion)
        {
            string path = null;
            
            using (var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var vsKey = view32.OpenSubKey(registryKeyName))
                {
                    if (vsKey != null)
                    {
                        using (RegistryKey versionKey = vsKey.OpenSubKey(visualStudioVersion))
                        {
                            if (versionKey == null)
                            {
                                throw new InvalidOperationException(string.Format("Expected key {0} to contain a subkey with name {1}", vsKey.Name, visualStudioVersion));
                            }

                            const string installdir = "InstallDir";
                            var installDir = versionKey.GetValue(installdir, null);

                            if (installDir == null || string.IsNullOrWhiteSpace(installDir.ToString()))
                            {
                                logger.WriteWarning(string.Format("Expected key {0} to contain a value with name {1} and a non-empty value", versionKey.Name, installdir));
                                return null;
                            }

                            var exePath = Path.Combine(installDir.ToString(), "CommonExtensions", "Microsoft",
                                                       "TestWindow",
                                                       "vstest.console.exe");

                            if (!File.Exists(exePath))
                            {
                                throw new InvalidOperationException(string.Format("The file '{0}' does not exist", exePath));
                            }

                            path = exePath;
                        }

                    }
                }
            }
            return path;
        }
    }
}