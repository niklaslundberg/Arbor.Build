using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Alphaleonis.Win32.Filesystem;

using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Extensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(650)]
    public class NuGetPacker : ITool
    {
        IReadOnlyCollection<string> _excludedNuSpecFiles;

        PathLookupSpecification _pathLookupSpecification;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageEnabled, defaultValue: true);

            if (!enabled)
            {
                logger.WriteWarning(
                    $"NuGet Packer is disabled (build variable '{WellKnownVariables.NuGetPackageEnabled}' is set to false");
                return ExitCode.Success;
            }

            _excludedNuSpecFiles =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageExcludesCommaSeparated, "")
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .SafeToReadOnlyCollection();

            _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string packageDirectory = PackageDirectory();

            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
            var packagesDirectory = Path.Combine(artifacts.Value, "packages");

            var vcsRootDir = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            NuGetPackageConfiguration packageConfiguration = NuGetPackager.GetNuGetPackageConfiguration(logger, buildVariables, packagesDirectory, vcsRootDir);

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


            var result =
                await ProcessPackagesAsync(packageSpecifications, packageConfiguration, logger, cancellationToken);

            return result;
        }


        IReadOnlyCollection<string> GetPackageSpecifications(ILogger logger, string vcsRootDir, string packageDirectory)
        {
            DirectoryInfo vcsRootDirectory = new DirectoryInfo(vcsRootDir);

            var packageSpecifications =
                vcsRootDirectory.GetFilesRecursive(new List<string> { ".nuspec" }, _pathLookupSpecification, vcsRootDir)
                    .Where(file => file.FullName.IndexOf(packageDirectory, StringComparison.Ordinal) < 0)
                    .Select(f => f.FullName)
                    .ToList();

            var pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            IReadOnlyCollection<FileInfo> filtered =
                packageSpecifications.Where(
                    packagePath => !pathLookupSpecification.IsFileBlackListed(packagePath, rootDir: vcsRootDir))
                    .Select(file => new FileInfo(file))
                    .ToReadOnlyCollection();

            var notExcluded =
                filtered.Where(
                    nuspec =>
                    !_excludedNuSpecFiles.Any(
                        exludedNuSpec => exludedNuSpec.Equals(nuspec.Name, StringComparison.InvariantCultureIgnoreCase)))
                    .SafeToReadOnlyCollection();

            logger.WriteVerbose(
                $"Found nuspec files [{filtered.Count}]: {Environment.NewLine}{string.Join(Environment.NewLine, filtered)}");
            var allIncluded = notExcluded.Select(file => file.FullName).SafeToReadOnlyCollection();

            return allIncluded;
        }

        static string PackageDirectory()
        {
            var packageDirectory = string.Format("{0}packages{0}", Path.DirectorySeparatorChar);
            return packageDirectory;
        }

        async Task<ExitCode> ProcessPackagesAsync(
            IEnumerable<string> packageSpecifications,
            NuGetPackageConfiguration packageConfiguration,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var packager = new NuGetPackager(logger);

            foreach (var packageSpecification in packageSpecifications)
            {
                var packageResult =
                    await packager.CreatePackageAsync(packageSpecification, packageConfiguration, cancellationToken: cancellationToken);

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