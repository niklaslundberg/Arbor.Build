using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.FS;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Serilog.Events;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[UsedImplicitly]
public class NuGetPackager
{
    public const string SnupkgPackageFormat = "snupkg";
    private readonly BuildContext _buildContext;
    private readonly IFileSystem _fileSystem;

    private readonly ILogger _logger;
    private readonly ManifestReWriter _manifestReWriter;

    public NuGetPackager(ILogger logger,
        BuildContext buildContext,
        ManifestReWriter manifestReWriter,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _buildContext = buildContext;
        _manifestReWriter = manifestReWriter;
        _fileSystem = fileSystem;
    }

    public NuGetPackageConfiguration? GetNuGetPackageConfiguration(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        DirectoryEntry packagesDirectory,
        DirectoryEntry vcsRootDir,
        string packageNameSuffix,
        string? runtimeIdentifier = null)
    {
        string version = buildVariables.Require(WellKnownVariables.Version).GetValueOrThrow()!;

        string branchName = buildVariables.Require(WellKnownVariables.BranchLogicalName).GetValueOrThrow()!;

        var branch = BranchName.TryParse(branchName);

        string? currentConfiguration =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.CurrentBuildConfiguration);

        string buildConfiguration = currentConfiguration ?? WellKnownConfigurations.Release;

        var tempDirectory = _fileSystem.GetDirectoryEntry(
            buildVariables.Require(WellKnownVariables.TempDirectory).ThrowIfEmptyValue().Value!.ParseAsPath());

        var nuGetExePath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
            .GetValueOrThrow().ParseAsPath();

        string? suffix =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix);

        bool buildNumberEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.BuildNumberInNuGetPackageArtifactsEnabled,
            true);

        bool keepBinaryAndSourcePackagesTogetherEnabled =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled,
                true);

        bool branchNameEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageIdBranchNameEnabled, true);

        string? packageIdOverride =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageIdOverride);

        bool nuGetSymbolPackagesEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSymbolPackagesEnabled);

        string? nuGetPackageVersionOverride =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageVersionOverride);

        bool allowManifestReWrite =
            buildVariables.GetBooleanByKey(WellKnownVariables.NuGetAllowManifestReWrite);

        bool buildPackagesOnAnyBranch =
            buildVariables.GetBooleanByKey(WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);

        string? gitModel =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitBranchModel);

        string? gitHash = buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitHash);

        if (!buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageArtifactsSuffixEnabled, true))
        {
            suffix = "";
        }

        bool parsed = GitBranchModel.TryParse(gitModel, out var model);

        if (parsed && model is {})
        {
            buildPackagesOnAnyBranch = true;
        }

        if (!buildPackagesOnAnyBranch)
        {
            if (BranchName.TryParse(branchName) is {IsMainBranch: true})
            {
                logger.Warning(
                    "NuGet package creation is not supported on 'master' branch. To force NuGet package creation, set environment variable '{NuGetCreatePackagesOnAnyBranchEnabled}' to value 'true'",
                    WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);

                return default;
            }
        }
        else
        {
            logger.Verbose("Flag '{NuGetCreatePackagesOnAnyBranchEnabled}' is set to true, creating NuGet packages",
                WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);
        }

        string packageFormat =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetSymbolPackageFormat,
                SnupkgPackageFormat)!;

        if (branch is null)
        {
            throw new InvalidOperationException(Resources.TheBranchNameCouldNotBeFound);
        }

        bool isReleaseBuild = IsStablePackage(branch);

        var options = new VersionOptions(version)
        {
            IsReleaseBuild = isReleaseBuild,
            BuildSuffix = suffix,
            BuildNumberEnabled = true,
            Logger = logger,
            GitModel = model,
            BranchName = branch
        };

        string semVer = NuGetVersionHelper.GetPackageVersion(options);

        var semanticVersion = SemanticVersion.Parse(semVer);

        if (!string.IsNullOrWhiteSpace(buildConfiguration)
            && buildConfiguration.Equals(WellKnownConfigurations.Debug, StringComparison.OrdinalIgnoreCase) &&
            isReleaseBuild)
        {
            logger.Information(
                "The current configuration is 'debug' but the build indicates that this is a release build, using 'release' configuration instead");
            buildConfiguration = WellKnownConfigurations.Release;
        }

        packagesDirectory.EnsureExists();

        if (!_fileSystem.FileExists(nuGetExePath))
        {
            logger.Error("The NuGet.exe path {NuGetExePath} was not found or NuGet could not be downloaded",
                nuGetExePath);
        }

        logger.Verbose("Scanning directory '{VcsRootDir}' for .nuspec files", vcsRootDir.ConvertPathToInternal());

        var packageConfiguration = new NuGetPackageConfiguration(
            buildConfiguration,
            semanticVersion,
            packagesDirectory,
            nuGetExePath,
            branchName,
            suffix,
            branchNameEnabled,
            packageIdOverride,
            nuGetPackageVersionOverride,
            allowManifestReWrite, nuGetSymbolPackagesEnabled, keepBinaryAndSourcePackagesTogetherEnabled,
            isReleaseBuild, buildNumberEnabled, tempDirectory, nuGetSymbolPackagesFormat: packageFormat,
            packageNameSuffix: packageNameSuffix, gitHash: gitHash,
            runtimeIdentifier: runtimeIdentifier);
        return packageConfiguration;
    }

    public async Task<ExitCode> CreatePackageAsync(
        [NotNull] FileEntry packageSpecificationPath,
        [NotNull] NuGetPackageConfiguration packageConfiguration,
        bool ignoreWarnings = false,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Using NuGet package configuration {PackageConfiguration}", packageConfiguration);

        var nuSpec = NuSpec.Parse(packageSpecificationPath);

        IDictionary<string, string?> properties = GetProperties(packageConfiguration);

        if (!string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
        {
            _logger.Information("Using NuGet package id override '{PackageIdOverride}'",
                packageConfiguration.PackageIdOverride);
        }

        string packageId =  NuGetPackageIdHelper.CreateNugetPackageId(nuSpec.PackageId, packageConfiguration);

        if (string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
        {
            _logger.Information("Using NuGet package ID {PackageId}", packageId);
        }
        else
        {
            _logger.Information("Using NuGet package version override '{PackageIdOverride}'",
                packageConfiguration.PackageIdOverride);
        }

        string nuGetPackageVersion = packageConfiguration.Version.ToNormalizedString();

        _logger.Information("{NuGetUsage}",
            string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride)
                ? $"Using NuGet package version {nuGetPackageVersion}"
                : $"Using NuGet package version override '{packageConfiguration.NuGetPackageVersionOverride}'");

        // ReSharper disable AssignNullToNotNullAttribute
        var nuSpecFileCopyPath =
            UPath.Combine(packageSpecificationPath.Directory.Path, $"{packageId}-{DateTime.Now.Ticks}_copy.nuspec");

        // ReSharper restore AssignNullToNotNullAttribute

        var nuSpecCopy = new NuSpec(packageId, packageConfiguration.Version, packageSpecificationPath);

        var nuSpecTempDirectory = UPath.Combine(packageConfiguration.TempPath.Path, "nuspecs");

        new DirectoryEntry(_fileSystem, nuSpecTempDirectory).EnsureExists();

        _logger.Verbose("Saving new nuspec '{NuSpecFileCopyPath}'", _fileSystem.ConvertPathToInternal(nuSpecFileCopyPath));

        var fileEntry = new FileEntry(_fileSystem, nuSpecFileCopyPath);
        await nuSpecCopy.Save(fileEntry);

        var removedTags = new List<string>();

        FileEntry? nuspec = null;

        if (packageConfiguration.AllowManifestReWrite)
        {
            _logger.Verbose("Rewriting manifest in NuSpec '{NuSpecFileCopyPath}'", _fileSystem.ConvertPathToInternal(nuSpecFileCopyPath));

            ManifestReWriteResult manifestReWriteResult = await
                _manifestReWriter.Rewrite(fileEntry, key =>
                {
                    if (!properties.TryGetValue(key, out string? value))
                    {
                        value = null;
                    }

                    _logger.Debug("Fetching nuspec property with key '{Key}', value '{Value}'", key, value ?? "N/A");

                    return value;
                });

            removedTags.AddRange(manifestReWriteResult.RemoveTags);

            nuspec = manifestReWriteResult.RewrittenNuSpec;
        }
        else
        {
            _logger.Verbose("Rewriting nuspec manifest is disabled");
        }

        nuspec ??= fileEntry;

        if (!nuspec.Exists)
        {
            _logger.Error("The nuspec file {NuSpecFile} does not exist", nuspec.ConvertPathToInternal());
            return ExitCode.Failure;
        }

        if (_logger.IsEnabled(LogEventLevel.Verbose))
        {
            string nuSpecContent = await GetNuSpecContent(nuspec);

            _logger.Verbose("Created nuspec content: {NewLine}{PackageContent}",
                Environment.NewLine,
                nuSpecContent);
        }

        var result = await ExecuteNuGetPackAsync(
            packageConfiguration.NuGetExePath,
            packageConfiguration.PackagesDirectory,
            _logger,
            nuspec,
            properties,
            nuSpecCopy,
            removedTags,
            cancellationToken: cancellationToken,
            nugetSymbolPackageEnabled: packageConfiguration.NuGetSymbolPackagesEnabled,
            symbolsFormat: packageConfiguration.NuGetSymbolPackagesFormat,
            ignoreWarnings: ignoreWarnings).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            string content = fileEntry.Exists ? await GetNuSpecContent(fileEntry) : "";
            _logger.Error("Could not create NuGet package from nuspec {NewLine}{NuSpec}",
                Environment.NewLine,
                content);
        }

        fileEntry.DeleteIfExists();
        nuspec.DeleteIfExists();

        return result;
    }

    private static async Task<string> GetNuSpecContent(FileEntry nuSpecFile)
    {
        await using var stream = nuSpecFile.Open(FileMode.Open, FileAccess.Read);

        return await stream.ReadAllTextAsync();
    }

    private static IDictionary<string, string?> GetProperties(NuGetPackageConfiguration configuration)
    {
        var propertyValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["configuration"] = configuration.Configuration,
            ["RuntimeIdentifier"] = configuration.RuntimeIdentifier,
            ["RepositoryCommit"] = configuration.GitHash
        };

        return propertyValues;
    }

    private bool IsStablePackage(BranchName branchName) =>
        (_buildContext.Configurations.Count == 1 &&
         _buildContext.Configurations.Single()
             .Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase))
        || branchName.IsProductionBranch();

    private async Task<ExitCode> ExecuteNuGetPackAsync(
        UPath nuGetExePath,
        DirectoryEntry packagesDirectoryPath,
        ILogger logger,
        FileEntry nuSpecFileCopyPath,
        IDictionary<string, string?> properties,
        NuSpec nuSpecCopy,
        List<string> removedTags,
        bool keepBinaryAndSourcePackagesTogetherEnabled = false,
        bool nugetSymbolPackageEnabled = false,
        bool ignoreWarnings = false,
        string symbolsFormat = "",
        CancellationToken cancellationToken = default)
    {
        bool hasRemovedNoSourceTag =
            removedTags.Any(
                tag => tag.Equals(WellKnownNuGetTags.NoSource, StringComparison.OrdinalIgnoreCase));

        ExitCode result;
        var arguments = new List<string>
        {
            "pack",
            _fileSystem.ConvertPathToInternal(nuSpecFileCopyPath.Path),
            "-OutputDirectory",
            _fileSystem.ConvertPathToInternal(packagesDirectoryPath.Path),
            "-NoPackageAnalysis",
            "-Version",
            nuSpecCopy.Version.ToNormalizedString()
        };

        foreach (var keyValuePair in properties)
        {
            arguments.Add("-Properties");
            arguments.Add($"{keyValuePair.Key}={keyValuePair.Value}");
        }

        if (!hasRemovedNoSourceTag && nugetSymbolPackageEnabled)
        {
            arguments.Add("-Symbols");

            if (!string.IsNullOrWhiteSpace(symbolsFormat) &&
                symbolsFormat.Equals(SnupkgPackageFormat, StringComparison.OrdinalIgnoreCase))
            {
                arguments.Add("-SymbolPackageFormat");
                arguments.Add(SnupkgPackageFormat);
            }
        }

        if (logger.IsEnabled(LogEventLevel.Verbose))
        {
            arguments.Add("-Verbosity");
            arguments.Add("Detailed");
        }

        if (ignoreWarnings)
        {
            arguments.Add("-NoPackageAnalysis");
        }

        var processResult =
            await
                ProcessRunner.ExecuteProcessAsync(
                    _fileSystem.ConvertPathToInternal(nuGetExePath),
                    arguments,
                    logger.Information,
                    logger.Error,
                    logger.Information,
                    cancellationToken: cancellationToken,
                    verboseAction: logger.Verbose,
                    debugAction: logger.Debug).ConfigureAwait(false);


        if (!keepBinaryAndSourcePackagesTogetherEnabled)
        {
            logger.Information(
                "The flag {NuGetKeepBinaryAndSymbolPackagesTogetherEnabled} is set to false, separating binary packages from symbol packages",
                WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled);

            var nugetPackages = packagesDirectoryPath.GetFiles("*.nupkg")
                .ToList();

            var nugetSymbolPackages = packagesDirectoryPath
                .GetFiles("*.symbols.nupkg")
                .ToList();

            var binaryPackages = nugetPackages.Except(nugetSymbolPackages).ToList();

            DirectoryEntry binaryPackagesDirectory =
                new DirectoryEntry(_fileSystem, UPath.Combine(packagesDirectoryPath.Path, "binary")).EnsureExists();

            DirectoryEntry symbolPackagesDirectory =
                new DirectoryEntry(_fileSystem, UPath.Combine(packagesDirectoryPath.Path, "symbol")).EnsureExists();

            foreach (var binaryPackage in binaryPackages)
            {
                var sourceFile = binaryPackage;
                var targetBinaryFile =
                    new FileEntry(_fileSystem, UPath.Combine(binaryPackagesDirectory.Path, sourceFile.Name));

                targetBinaryFile.DeleteIfExists();

                logger.Debug("Copying NuGet binary package '{BinaryPackage}' to '{TargetBinaryFile}'",
                    _fileSystem.ConvertPathToInternal(binaryPackage.Path),
                    _fileSystem.ConvertPathToInternal(targetBinaryFile.Path));
                sourceFile.MoveTo(targetBinaryFile.Path);
            }

            foreach (var sourcePackage in nugetSymbolPackages)
            {
                var sourceFile = sourcePackage;
                var targetSymbolFile =
                    new FileEntry(_fileSystem, UPath.Combine(symbolPackagesDirectory.Path, sourceFile.Name));

                if (targetSymbolFile.Exists)
                {
                    targetSymbolFile.Delete();
                }

                logger.Debug("Copying NuGet symbol package '{SourcePackage}' to '{TargetSymbolFile}'",
                    _fileSystem.ConvertPathToInternal(sourcePackage.Path),
                    _fileSystem.ConvertPathToInternal(targetSymbolFile.Path));

                sourceFile.MoveTo(targetSymbolFile.FullName);
            }
        }

        result = processResult;

        return result;
    }
}