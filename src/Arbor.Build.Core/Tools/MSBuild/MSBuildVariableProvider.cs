using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Defensive.Collections;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Microsoft.Win32;
using Newtonsoft.Json;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.MSBuild
{
    [UsedImplicitly]
    public class MSBuildVariableProvider : IVariableProvider
    {
        private readonly IEnvironmentVariables _environmentVariables;
        private readonly ISpecialFolders _specialFolders;
        private readonly IFileSystem _fileSystem;

        public MSBuildVariableProvider(IEnvironmentVariables environmentVariables, ISpecialFolders specialFolders, IFileSystem fileSystem)
        {
            _environmentVariables = environmentVariables;
            _specialFolders = specialFolders;
            _fileSystem = fileSystem;
        }

        private async Task<ImmutableArray<IVariable>> TryGetWithVsWhereAsync(
            UPath vsWherePath,
            string command,
            string component,
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            if (_fileSystem.FileExists(vsWherePath))
            {
                logger.Debug("vswhere.exe exists at '{VsWherePath}'", _fileSystem.ConvertPathToInternal(vsWherePath));

                ExitCode versionExitCode = await ProcessHelper.ExecuteAsync(
                   _fileSystem.ConvertPathToInternal(vsWherePath),
                    new List<string> { "-prerelease" },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var vsWhereArgs = new List<string> { command, component, "-format", "json" };

                bool allowPreRelease =
                    buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_AllowPreReleaseEnabled)
                    || buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_VisualStudio_Version_Allow_PreRelease);

                // NOTE only newer releases of vswhere.exe supports -prerelease flag
                if (allowPreRelease && versionExitCode.IsSuccess)
                {
                    vsWhereArgs.Add("-prerelease");
                }

                var resultBuilder = new StringBuilder();

                void StandardOutLog(string message, string _) => resultBuilder.Append(message);

                ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(
                  _fileSystem.ConvertPathToInternal( vsWherePath),
                    vsWhereArgs,
                    StandardOutLog,
                    cancellationToken: cancellationToken,
                    toolAction: logger.Debug,
                    standardErrorAction: logger.Error).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    logger.Error("Could not get VS version by using vswhere, exit code {ExitCode}", exitCode.Code);
                    return ImmutableArray<IVariable>.Empty;
                }

                string json = resultBuilder.ToString();

                if (string.IsNullOrWhiteSpace(json))
                {
                    logger.Error("Could not get VS version by using vswhere, empty json response");
                    return ImmutableArray<IVariable>.Empty;
                }

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

                    var candidates = typedInstallations.SafeToReadOnlyCollection();

                    if (!allowPreRelease)
                    {
                        candidates = candidates
                            .Where(candidate =>
                                candidate.channelId.IndexOf("preview", StringComparison.OrdinalIgnoreCase) < 0)
                            .ToImmutableArray();
                    }

                    var array = candidates
                        .Select(candidate => new { candidate, version = Version.Parse(candidate.installationVersion) })
                        .ToArray();

                    var latest = array.OrderByDescending(candidateItem => candidateItem.version)
                        .FirstOrDefault();

                    logger.Debug("Found VS candidate version with vswhere.exe: {Paths}", candidates.Select(s => s.installationPath).ToArray());

                    if (latest != null)
                    {
                        var msbuild2019Path = UPath.Combine(
                            latest.candidate.installationPath.ParseAsPath(),
                            "MSBuild",
                            "Current",
                            "bin",
                            "MSBuild.exe");

                        if (_fileSystem.FileExists(msbuild2019Path))
                        {
                            logger.Information("Found MSBuild with vswhere.exe at '{MsbuildPath}'",  _fileSystem.ConvertPathToInternal(msbuild2019Path));

                            IVariable[] variables =
                            {
                                new BuildVariable(
                                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                                    _fileSystem.ConvertPathToInternal(msbuild2019Path))
                            };

                            return variables.ToImmutableArray();
                        }

                        var msbuild2017Path = UPath.Combine(
                            latest.candidate.installationPath.ParseAsPath(),
                            "MSBuild",
                            "15.0",
                            "bin",
                            "MSBuild.exe");

                        if (_fileSystem.FileExists(msbuild2017Path))
                        {
                            logger.Information("Found MSBuild with vswhere.exe at '{MsbuildPath}'",  _fileSystem.ConvertPathToInternal(msbuild2017Path));

                            IVariable[] variables =
                            {
                                new BuildVariable(
                                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                                    _fileSystem.ConvertPathToInternal(msbuild2017Path))
                            };

                            return variables.ToImmutableArray();
                        }

                        logger.Debug("Could not find VS 2017 or 2019 MSBuild path for candidate {Candidate}", latest.candidate.installationPath);
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

            return ImmutableArray<IVariable>.Empty;
        }

        public int Order => VariableProviderOrder.Ignored;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
#pragma warning disable CA1416 // Validate platform compatibility

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return ImmutableArray<IVariable>.Empty;
            }

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled))
            {
                return ImmutableArray<IVariable>.Empty;
            }

            string? path = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_ExePath);

            if (!string.IsNullOrWhiteSpace(path))
            {
                return ImmutableArray<IVariable>.Empty;
            }

            int currentProcessBits = Environment.Is64BitProcess ? 64 : 32;
            const int registryLookupBits = 32;
            logger.Verbose("Running current process [id {Id}] as a {CurrentProcessBits}-bit process",
                Process.GetCurrentProcess().Id,
                currentProcessBits);

            var possibleMajorVersions = new List<string>
                {
                    "16.0.0",
                    "15.0.0",
                    "14.0.0",
                    "12.0.0",
                    "4.0.0"
                }
                .Select(SemanticVersion.Parse)
                .ToList();

            string? max = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_MSBuild_MaxVersion,
                "16.99.0");

            SemanticVersion[] toRemove = possibleMajorVersions.Where(version => version > SemanticVersion.Parse(max))
                .ToArray();

            foreach (SemanticVersion semVersion in toRemove)
            {
                possibleMajorVersions.Remove(semVersion);
            }

            var vsWherePath = UPath.Combine(
                _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                "Microsoft Visual Studio",
                "Installer",
                "vswhere.exe");

            if (_fileSystem.FileExists(vsWherePath))
            {
                ImmutableArray<IVariable> variables = await TryGetWithVsWhereAsync(vsWherePath,
                    "-requires",
                    "Microsoft.Component.MSBuild",
                    logger,
                    buildVariables,
                    cancellationToken).ConfigureAwait(false);

                if (variables.Any())
                {
                    return variables;
                }

                ImmutableArray<IVariable> variablesForTools = await TryGetWithVsWhereAsync(vsWherePath,
                    " ",
                    "Microsoft.VisualStudio.Product.BuildTools",
                    logger,
                    buildVariables,
                    cancellationToken).ConfigureAwait(false);

                if (variablesForTools.Any())
                {
                    return variablesForTools;
                }
            }

            UPath[] possiblePaths =
            {
                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                    "Microsoft Visual Studio",
                    "2019",
                    "Enterprise",
                    "MSBuild",
                    "Current",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                    "Microsoft Visual Studio",
                    "2019",
                    "Professional",
                    "MSBuild",
                    "Current",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                    "Microsoft Visual Studio",
                    "2019",
                    "Community",
                    "MSBuild",
                    "Current",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                    "Microsoft Visual Studio",
                    "2019",
                    "BuildTools",
                    "MSBuild",
                    "Current",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                    "Microsoft Visual Studio",
                    "2017",
                    "Enterprise",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "Professional",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "Community",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe"),

                UPath.Combine(
                    _specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio",
                    "2017",
                    "BuildTools",
                    "MSBuild",
                    "15.0",
                    "bin",
                    "MSBuild.exe")
            };

            var fileBasedLookupResultPath = possiblePaths.FirstOrDefault(_fileSystem.FileExists);

            if (fileBasedLookupResultPath is {})
            {
                logger.Information("Found MSBuild at '{FileBasedLookupResultPath}'", _fileSystem.ConvertPathToInternal(fileBasedLookupResultPath));

                IVariable[] variables =
                {
                    new BuildVariable(
                        WellKnownVariables.ExternalTools_MSBuild_ExePath,
                        _fileSystem.ConvertPathToInternal(fileBasedLookupResultPath.FullName))
                };

                return variables.ToImmutableArray();
            }

            logger.Debug("Could not find MSBuild.exe in any of paths {Paths}", possiblePaths.Select(path => _fileSystem.ConvertPathToInternal(path)).ToArray());

            string? foundPath = null;

            foreach (SemanticVersion possibleVersion in possibleMajorVersions)
            {
                for (int i = 99; i >= 0; i--)
                {
                    int minorVersion = i;
                    string registryKeyName =
                        $@"SOFTWARE\Microsoft\MSBuild\{possibleVersion.Major}.{minorVersion}";
                    object? msBuildPathRegistryKeyValue = null;
                    const string valueKey = "MSBuildOverrideTasksPath";

                    logger.Verbose(
                        "Looking for MSBuild exe path in {RegistryLookupBits}-bit registry key '{RegistryKeyName}\\{ValueKey}",
                        registryLookupBits,
                        registryKeyName,
                        valueKey);

                    using var view32 =
                        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                    RegistryKey? key = view32.OpenSubKey(registryKeyName);

                    if (key != null)
                    {
                        using (key)
                        {
                            msBuildPathRegistryKeyValue = key.GetValue(valueKey, null);
                        }
                    }

                    string? msBuildPath = msBuildPathRegistryKeyValue != null
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
            }

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                const string msbuildPath = "MSBUILD_PATH";
                string? fromEnvironmentVariable = _environmentVariables.GetEnvironmentVariable(msbuildPath);

                if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
                {
                    logger.Information("Using MSBuild exe path '{FoundPath}' from environment variable {MsbuildPath}",
                        _fileSystem.ConvertPathToInternal(foundPath.ParseAsPath()),
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

            logger.Information("Using MSBuild exe path '{FoundPath}'", _fileSystem.ConvertPathToInternal(foundPath.ParseAsPath()));

            IVariable[] environmentVariables =
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_MSBuild_ExePath,
                    _fileSystem.ConvertPathToInternal(foundPath.ParseAsPath()))
            };

#pragma warning restore CA1416 // Validate platform compatibility

            return environmentVariables.ToImmutableArray();
        }
    }
}
