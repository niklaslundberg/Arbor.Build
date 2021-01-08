using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Packaging;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(820)]
    [UsedImplicitly]
    public class VersionReporter : ITool
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public VersionReporter(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public Task<ExitCode> ExecuteAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var nuGetPackageFiles = new List<FileEntry>();

            var artifactPackagesDirectory = new DirectoryEntry(_fileSystem, artifacts.Value!.ParseAsPath());

            if (!artifactPackagesDirectory.Exists)
            {
                logger.Warning("There is no packages folder, skipping standard package upload");
            }
            else
            {
                var standardPackages =
                    artifactPackagesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                        .Where(file => file.Name.IndexOf("symbols", StringComparison.OrdinalIgnoreCase) < 0)
                        .ToList();

                nuGetPackageFiles.AddRange(standardPackages);
            }

            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var nuGetPackageFile in nuGetPackageFiles)
            {
                using var package = new PackageArchiveReader(nuGetPackageFile.FileSystem.ConvertPathToInternal(nuGetPackageFile.FullName));
                versions.Add(SemanticVersion.Parse(package.NuspecReader.GetVersion().ToNormalizedString()).ToNormalizedString());
            }

            bool? isRunningInTeamCity =
                buildVariables.GetOptionalBooleanByKey(WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity);

            if (versions.Count == 1)
            {
                if (isRunningInTeamCity == true)
                {
                    _logger.Verbose("Setting TeamCity variable env.{Key}",
                        WellKnownVariables.NuGetPackageVersionResult);
                    string? version = versions.Single();
                    _logger.Information(
                        "##teamcity[setParameter name='env." + WellKnownVariables.NuGetPackageVersionResult +
                        "' value='{Version}']", version);
                }
                else
                {
                    _logger.Verbose("Build is not running i TeamCity, skipping sending version message");
                }
            }
            else
            {
                _logger.Debug("Found multiple NuGet package versions {Versions}, could not set nugetPackageVersion", versions);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}