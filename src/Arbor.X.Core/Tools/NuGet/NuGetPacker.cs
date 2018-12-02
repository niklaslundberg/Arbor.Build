using System;
using System.Collections.Generic;
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
        private IReadOnlyCollection<string> _excludedNuSpecFiles;

        private PathLookupSpecification _pathLookupSpecification;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
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
                        string.Empty)
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .SafeToReadOnlyCollection();

            _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string packageDirectory = PackageDirectory();

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
            string packagesDirectory = Path.Combine(artifacts.Value, "packages");

            string vcsRootDir = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            NuGetPackageConfiguration packageConfiguration =
                NuGetPackager.GetNuGetPackageConfiguration(logger, buildVariables, packagesDirectory, vcsRootDir);

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
            string packageDirectory = string.Format("{0}packages{0}", Path.DirectorySeparatorChar);
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
                        packagePath => !pathLookupSpecification.IsFileBlackListed(packagePath, vcsRootDir).Item1)
                    .Select(file => new FileInfo(file))
                    .ToReadOnlyCollection();

            IReadOnlyCollection<FileInfo> notExcluded =
                filtered.Where(
                        nuspec =>
                            !_excludedNuSpecFiles.Any(
                                exludedNuSpec => exludedNuSpec.Equals(
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
            var packager = new NuGetPackager(logger);

            foreach (string packageSpecification in packageSpecifications)
            {
                ExitCode packageResult =
                    await packager.CreatePackageAsync(
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
