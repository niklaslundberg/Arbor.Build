using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(650)]
    [UsedImplicitly]
    public class NuGetPacker : ITool
    {
        private IReadOnlyCollection<string> _excludedNuSpecFiles = ImmutableArray<string>.Empty;

        private PathLookupSpecification _pathLookupSpecification = null!;
        private readonly NuGetPackager _nugetPackager;

        public NuGetPacker(NuGetPackager nuGetPackager) => _nugetPackager = nuGetPackager;

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

            _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string packageDirectory = PackageDirectory();

            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).GetValueOrThrow();
            string packagesDirectory = Path.Combine(artifacts, "packages");

            string vcsRootDir = buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow();

            NuGetPackageConfiguration? packageConfiguration =
                _nugetPackager.GetNuGetPackageConfiguration(logger, buildVariables, packagesDirectory, vcsRootDir, "");

            if (packageConfiguration is null)
            {
                return ExitCode.Success;
            }

            IReadOnlyCollection<string> packageSpecifications = GetPackageSpecifications(
                logger,
                vcsRootDir,
                packageDirectory);

            if (packageSpecifications.Count == 0)
            {
                logger.Information("Could not find any NuGet specifications to create NuGet packages from");
                return ExitCode.Success;
            }

            logger.Information("Found {Count} NuGet specifications to create NuGet packages from",
                packageSpecifications.Count);

            ExitCode result;

            int timeoutInSeconds = buildVariables.GetInt32ByKey(WellKnownVariables.NuGetPackageTimeoutInSeconds, defaultValue: 60);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds)))
            {
                using (CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,cts.Token))
                {
                    result =
                        await ProcessPackagesAsync(packageSpecifications,
                                packageConfiguration,
                                logger,
                                cancellationToken)
                            .ConfigureAwait(false);
                }
            }

            return result;
        }

        private static string PackageDirectory()
        {
            string packageDirectory = $"{Path.DirectorySeparatorChar}packages{Path.DirectorySeparatorChar}";
            return packageDirectory;
        }

        private IReadOnlyCollection<string> GetPackageSpecifications(
            ILogger logger,
            string vcsRootDir,
            string packageDirectory)
        {
            var vcsRootDirectory = new DirectoryInfo(vcsRootDir);

            List<string> packageSpecifications =
                vcsRootDirectory.GetFilesRecursive(new List<string> { ".nuspec" }, _pathLookupSpecification, vcsRootDir)
                    .Where(file => file.FullName.IndexOf(packageDirectory, StringComparison.Ordinal) < 0)
                    .Select(f => f.FullName)
                    .ToList();

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            IReadOnlyCollection<FileInfo> filtered =
                packageSpecifications.Where(
                        packagePath => !pathLookupSpecification.IsFileExcluded(packagePath, vcsRootDir).Item1)
                    .Select(file => new FileInfo(file))
                    .ToReadOnlyCollection();

            IReadOnlyCollection<FileInfo> notExcluded =
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
                string.Join(Environment.NewLine, filtered));

            IReadOnlyCollection<string> allIncluded = notExcluded.Select(file => file.FullName)
                .SafeToReadOnlyCollection();

            return allIncluded;
        }

        private async Task<ExitCode> ProcessPackagesAsync(
            IEnumerable<string> packageSpecifications,
            NuGetPackageConfiguration packageConfiguration,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            foreach (string packageSpecification in packageSpecifications)
            {
                ExitCode packageResult =
                    await _nugetPackager.CreatePackageAsync(
                        packageSpecification,
                        packageConfiguration,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!packageResult.IsSuccess)
                {
                    logger.Error("Could not create NuGet package from specification '{PackageSpecification}'",
                        packageSpecification);
                    return packageResult;
                }
            }

            return ExitCode.Success;
        }
    }
}
