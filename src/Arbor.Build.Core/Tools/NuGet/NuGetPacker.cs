﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Defensive.Collections;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(650)]
[UsedImplicitly]
public class NuGetPacker : ITool
{
    private IReadOnlyCollection<string> _excludedNuSpecFiles = ImmutableArray<string>.Empty;

    private PathLookupSpecification _pathLookupSpecification = null!;
    private readonly NuGetPackager _nugetPackager;
    private readonly IFileSystem _fileSystem;
    private readonly BuildContext _buildContext;

    public NuGetPacker(NuGetPackager nuGetPackager, IFileSystem fileSystem, BuildContext buildContext)
    {
        _nugetPackager = nuGetPackager;
        _fileSystem = fileSystem;
        _buildContext = buildContext;
    }

    public async Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageEnabled, true);

        if (!enabled)
        {
            logger.Warning("NuGet Packer is disabled (build variable '{NuGetPackageEnabled}' is set to false",
                WellKnownVariables.NuGetPackageEnabled);
            return ExitCode.Success;
        }

        _excludedNuSpecFiles =
            buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.NuGetPackageExcludesCommaSeparated,
                    string.Empty)!
                .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .SafeToReadOnlyCollection();

        _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new []
        {
            "packages.lock.json",
            "config.user"
        });

        string? artifacts = buildVariables.Require(WellKnownVariables.Artifacts).GetValueOrThrow();
        var packagesDirectory = new DirectoryEntry(_fileSystem, UPath.Combine(artifacts.ParseAsPath(), "packages"));

        DirectoryEntry vcsRootDir = _buildContext.SourceRoot;

        var runtimeIdentifier = buildVariables.GetVariableValueOrDefault(WellKnownVariables.PublishRuntimeIdentifier, string.Empty);

        NuGetPackageConfiguration? packageConfiguration =
            _nugetPackager.GetNuGetPackageConfiguration(logger, buildVariables, packagesDirectory, vcsRootDir, "", runtimeIdentifier);

        if (packageConfiguration is null)
        {
            return ExitCode.Success;
        }

        IReadOnlyCollection<FileEntry> packageSpecifications = GetPackageSpecifications(
            logger,
            vcsRootDir,
            packagesDirectory);

        if (packageSpecifications.Count == 0)
        {
            logger.Information("Could not find any NuGet specifications to create NuGet packages from");
            return ExitCode.Success;
        }

        logger.Information("Found {Count} NuGet specifications to create NuGet packages from",
            packageSpecifications.Count);

        ExitCode result;

        int timeoutInSeconds = buildVariables.GetInt32ByKey(WellKnownVariables.NuGetPackageTimeoutInSeconds, defaultValue: 60);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));
        using (CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,cts.Token))
        {
            result =
                await ProcessPackagesAsync(packageSpecifications,
                        packageConfiguration,
                        logger,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        return result;
    }

    private IReadOnlyCollection<FileEntry> GetPackageSpecifications(
        ILogger logger,
        DirectoryEntry vcsRootDir,
        DirectoryEntry packageDirectory)
    {
        var packageSpecifications =
            vcsRootDir.GetFilesRecursive(new List<string> { ".nuspec" }, _pathLookupSpecification, vcsRootDir)
                .Where(file => file.FullName.IndexOf(packageDirectory.FullName, StringComparison.Ordinal) < 0)
                .ToList();

        PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

        IReadOnlyCollection<FileEntry> filtered =
            packageSpecifications.Where(
                    packagePath => !pathLookupSpecification.IsFileExcluded(packagePath, vcsRootDir).Item1)

                .ToReadOnlyCollection();

        IReadOnlyCollection<FileEntry> notExcluded =
            filtered.Where(
                    nuspec =>
                        !_excludedNuSpecFiles.Any(
                            excludedNuSpec => excludedNuSpec.Equals(
                                nuspec.Name,
                                StringComparison.OrdinalIgnoreCase)))
                .SafeToReadOnlyCollection();

        logger.Verbose("Found nuspec files [{Count}]: {NewLine}{V}",
            filtered.Count,
            Environment.NewLine,
            string.Join(Environment.NewLine, filtered.Select(file => file.ConvertPathToInternal())));

        IReadOnlyCollection<FileEntry> allIncluded = notExcluded.Select(file => file)
            .SafeToReadOnlyCollection();

        return allIncluded;
    }

    private async Task<ExitCode> ProcessPackagesAsync(
        IEnumerable<FileEntry> packageSpecifications,
        NuGetPackageConfiguration packageConfiguration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var packageSpecification in packageSpecifications)
        {
            ExitCode packageResult =
                await _nugetPackager.CreatePackageAsync(
                    packageSpecification,
                    packageConfiguration,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!packageResult.IsSuccess)
            {
                logger.Error("Could not create NuGet package from specification '{PackageSpecification}'",
                    packageSpecification.ConvertPathToInternal());
                return packageResult;
            }
        }

        return ExitCode.Success;
    }
}