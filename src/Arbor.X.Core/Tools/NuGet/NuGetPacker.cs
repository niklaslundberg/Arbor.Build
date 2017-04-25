using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
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
                logger.WriteWarning(
                    $"NuGet Packer is disabled (build variable '{WellKnownVariables.NuGetPackageEnabled}' is set to false");
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

            if (!packageSpecifications.Any())
            {
                logger.Write("Could not find any NuGet specifications to create NuGet packages from");
                return ExitCode.Success;
            }

            logger.Write($"Found {packageSpecifications.Count} NuGet specifications to create NuGet packages from");

            ExitCode result =
                await ProcessPackagesAsync(packageSpecifications, packageConfiguration, logger, cancellationToken);

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
                        packagePath => !pathLookupSpecification.IsFileBlackListed(packagePath, vcsRootDir))
                    .Select(file => new FileInfo(file))
                    .ToReadOnlyCollection();

            IReadOnlyCollection<FileInfo> notExcluded =
                filtered.Where(
                        nuspec =>
                            !_excludedNuSpecFiles.Any(
                                exludedNuSpec => exludedNuSpec.Equals(
                                    nuspec.Name,
                                    StringComparison.InvariantCultureIgnoreCase)))
                    .SafeToReadOnlyCollection();

            logger.WriteVerbose(
                $"Found nuspec files [{filtered.Count}]: {Environment.NewLine}{string.Join(Environment.NewLine, filtered)}");
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
                        cancellationToken: cancellationToken);

                if (!packageResult.IsSuccess)
                {
                    logger.WriteError($"Could not create NuGet package from specification '{packageSpecification}'");
                    return packageResult;
                }
            }

            return ExitCode.Success;
        }
    }
}
