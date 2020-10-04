﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Microsoft.Win32;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.VisualStudio
{
    [UsedImplicitly]
    public class VisualStudioVariableProvider : IVariableProvider
    {
        private bool _allowPreReleaseVersions;
        private readonly IFileSystem _fileSystem;

        public VisualStudioVariableProvider(IFileSystem fileSystem) => _fileSystem = fileSystem;

        public int Order => VariableProviderOrder.Ignored;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_VisualStudio_Version,
                string.Empty)))
            {
                return Task.FromResult(ImmutableArray<IVariable>.Empty);
            }

            _allowPreReleaseVersions =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_VisualStudio_Version_Allow_PreRelease);

            int currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.Verbose("Running current process [id {Id}] as a {CurrentProcessBits}-bit process",
                Process.GetCurrentProcess().Id,
                currentProcessBits);

            const string registryKeyName = @"SOFTWARE\Microsoft\VisualStudio";

            logger.Verbose(
                "Looking for Visual Studio versions in {RegistryLookupBits}-bit registry key 'HKEY_LOCAL_MACHINE\\{RegistryKeyName}'",
                registryLookupBits,
                registryKeyName);

            string? visualStudioVersion = GetVisualStudioVersion(logger, registryKeyName);

            UPath? vsTestExePath = null;

            if (!string.IsNullOrWhiteSpace(visualStudioVersion))
            {
                logger.Verbose("Found Visual Studio version {VisualStudioVersion}", visualStudioVersion);

                vsTestExePath = GetVSTestExePath(logger, registryKeyName, visualStudioVersion);
            }
            else
            {
                logger.Warning("Could not find any Visual Studio version");
            }

            var environmentVariables = new IVariable[]
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_VisualStudio_Version,
                    visualStudioVersion),
                new BuildVariable(WellKnownVariables.ExternalTools_VSTest_ExePath, vsTestExePath?.FullName)
            };

            return Task.FromResult(environmentVariables.ToImmutableArray());
        }

        private UPath? GetVSTestExePath(ILogger logger, string registryKeyName, string visualStudioVersion)
        {
            UPath? path = null;

            using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using RegistryKey? vsKey = view32.OpenSubKey(registryKeyName);
                if (vsKey != null)
                {
                    using RegistryKey? versionKey = vsKey.OpenSubKey(visualStudioVersion);
                    if (versionKey == null)
                    {
                        throw new InvalidOperationException(
                            $"Expected key {vsKey.Name} to contain a subkey with name {visualStudioVersion}");
                    }

                    const string installDirKey = "InstallDir";
                    string? installDir = versionKey.GetValue(installDirKey, null)?.ToString();

                    if (string.IsNullOrWhiteSpace(installDir))
                    {
                        logger.Warning(
                            "Expected key {Name} to contain a value with name {InstallDir} and a non-empty value",
                            versionKey.Name,
                            installDirKey);
                        return null;
                    }

                    UPath exePath = UPath.Combine(
                        installDir,
                        "CommonExtensions",
                        "Microsoft",
                        "TestWindow",
                        "vstest.console.exe");

                    if (!_fileSystem.FileExists(exePath))
                    {
                        throw new InvalidOperationException($"The file '{exePath}' does not exist");
                    }

                    path = exePath;
                }
            }

            return path;
        }

        private string? GetVisualStudioVersion(ILogger logger, string registryKeyName)
        {
            string? visualStudioVersion = null;

            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using RegistryKey? vsKey = view32.OpenSubKey(registryKeyName);
            if (vsKey is {})
            {
                List<Version> versions = vsKey.GetSubKeyNames()
                    .Where(subKeyName => char.IsDigit(subKeyName.First()))
                    .Select(
                        keyName =>
                        {
                            if (!Version.TryParse(keyName, out Version? version) && _allowPreReleaseVersions)
                            {
                                const string preReleaseSeparator = "_";

                                int indexOf = keyName.IndexOf(
                                    preReleaseSeparator,
                                    StringComparison.OrdinalIgnoreCase);

                                if (indexOf >= 0)
                                {
                                    string versionOnly = keyName.Substring(0, indexOf);

                                    if (Version.TryParse(versionOnly, out version))
                                    {
                                        logger.Debug("Found pre-release Visual Studio version {Version}",
                                            version!);
                                    }
                                }
                            }

                            if (version is null)
                            {
                                logger.Debug(
                                    "Could not parse Visual Studio version from registry key name '{KeyName}', skipping that version.",
                                    keyName);
                            }

                            return version;
                        })
                    .Where(version => version is {})
                    .OrderByDescending(name => name)
                    .ToList()!;

                logger.Verbose("Found {Count} Visual Studio versions: {Versions}",
                    versions.Count,
                    string.Join(", ", versions.Select(version => version.ToString(2))));

                if (versions.Any(version => version == new Version(15, 0)))
                {
                    visualStudioVersion = "15.0";
                }

                if (versions.Any(version => version == new Version(14, 0)))
                {
                    visualStudioVersion = "14.0";
                }
                else if (versions.Any(version => version == new Version(12, 0)))
                {
                    visualStudioVersion = "12.0";
                }
                else if (versions.Any(version => version == new Version(11, 0)))
                {
                    visualStudioVersion = "11.0";
                }
                else if (versions.Count > 0)
                {
                    visualStudioVersion = versions.First().ToString(2);
                }
            }

            return visualStudioVersion;
        }
    }
}
