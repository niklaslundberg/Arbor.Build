using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Castanea;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(100)]
    public class NuGetRestorer : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var app = new CastaneaApplication();

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            var vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            var nuGetExetPath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue().Value;

            var rootDirectory = new DirectoryInfo(vcsRoot);

            var packagesConfigFiles = rootDirectory.EnumerateFiles("packages.config", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName))
                .Select(file => file.FullName)
                .ToReadOnlyCollection();

            var solutionFiles = rootDirectory.EnumerateFiles("*.sln", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName))
                .ToReadOnlyCollection();

            if (!packagesConfigFiles.Any())
            {
                logger.WriteWarning("Could not find any packages.config files, skipping package restore");
                return Task.FromResult(ExitCode.Success);
            }

            if (!solutionFiles.Any())
            {
                logger.WriteError("Could not find any solution file, cannot determine package output directory");
                return Task.FromResult(ExitCode.Failure);
            }

            if (solutionFiles.Count > 1)
            {
                logger.WriteError("Found more than one solution file, cannot determine package output directory"); ;
                return Task.FromResult(ExitCode.Failure);
            }

            var solutionFile = solutionFiles.Single();

            var allFiles = string.Join(Environment.NewLine, packagesConfigFiles);
            try
            {
// ReSharper disable once PossibleNullReferenceException
                string outputDirectoryPath = Path.Combine(solutionFile.Directory.FullName, "packages");

                new DirectoryInfo(outputDirectoryPath).EnsureExists();

                var nuGetConfig = new NuGetConfig
                                  {
                                      NuGetExePath = nuGetExetPath,
                                      OutputDirectory = outputDirectoryPath
                                  };

                nuGetConfig.PackageConfigFiles.AddRange(packagesConfigFiles);

                int restoredPackages = app.RestoreAllSolutionPackages(nuGetConfig);

                if (restoredPackages == 0)
                {
                    logger.WriteWarning(string.Format("No packages was restored as defined in {0}", allFiles));
                    return Task.FromResult(ExitCode.Success);
                }

                logger.Write(string.Format("Restored {0} package configurations defined in {1}", restoredPackages, allFiles));
            }
            catch (Exception ex)
            {
                logger.WriteError(string.Format("Cloud not restore packages defined in '{0}'. {1}", allFiles, ex));
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}