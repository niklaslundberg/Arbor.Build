using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Castanea;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Exceptions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(100)]
    public class NuGetRestorer : ITool
    {
        private readonly IReadOnlyCollection<INuGetPackageRestoreFix> _fixes;

        public NuGetRestorer(IEnumerable<INuGetPackageRestoreFix> fixes)
        {
            _fixes = fixes.SafeToReadOnlyCollection();
        }

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var app = new CastaneaApplication();

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            var vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            var nuGetExetPath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue().Value;

            var rootDirectory = new DirectoryInfo(vcsRoot);

            var packagesConfigFiles = rootDirectory.EnumerateFiles("packages.config", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, rootDir: vcsRoot))
                .Select(file => file.FullName)
                .ToReadOnlyCollection();

            var solutionFiles = rootDirectory.EnumerateFiles("*.sln", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, rootDir: vcsRoot))
                .ToReadOnlyCollection();

            if (!packagesConfigFiles.Any())
            {
                logger.WriteWarning("Could not find any packages.config files, skipping package restore");
                return ExitCode.Success;
            }

            if (!solutionFiles.Any())
            {
                logger.WriteError("Could not find any solution file, cannot determine package output directory");
                return ExitCode.Failure;
            }

            if (solutionFiles.Count > 1)
            {
                logger.WriteError("Found more than one solution file, cannot determine package output directory"); ;
                return ExitCode.Failure;
            }

            var solutionFile = solutionFiles.Single();

            var allFiles = string.Join(Environment.NewLine, packagesConfigFiles);
            try
            {
// ReSharper disable once PossibleNullReferenceException
                string outputDirectoryPath = Path.Combine(solutionFile.Directory.FullName, "packages");

                new DirectoryInfo(outputDirectoryPath).EnsureExists();

                bool disableParallelProcessing =
                    buildVariables.GetBooleanByKey(WellKnownVariables.NuGetRestoreDisableParallelProcessing,
                        defaultValue: false);

                bool noCache = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetRestoreNoCache,
                    defaultValue: false);

                var nuGetConfig = new NuGetConfig
                                  {
                                      NuGetExePath = nuGetExetPath,
                                      OutputDirectory = outputDirectoryPath,
                                      DisableParallelProcessing = disableParallelProcessing,
                                      NoCache = noCache
                                  };

                nuGetConfig.PackageConfigFiles.AddRange(packagesConfigFiles);

                Action<string> debugAction = null;

                string prefix = typeof(CastaneaApplication).Namespace;

                if (logger.LogLevel.IsLogging(LogLevel.Verbose))
                {
                    debugAction = message => logger.WriteVerbose(message, prefix);
                }

                int restoredPackages = app.RestoreAllSolutionPackages(nuGetConfig, 
                    logInfo: message => logger.Write(message, prefix),
                    logError: message => logger.WriteError(message, prefix),
                    logDebug: debugAction);

                if (restoredPackages == 0)
                {
                    logger.WriteWarning(string.Format("No packages was restored as defined in {0}", allFiles));
                    return ExitCode.Success;
                }

                logger.Write(string.Format("Restored {0} package configurations defined in {1}", restoredPackages, allFiles));
            }
            catch (Exception ex)
            {
                logger.WriteError(string.Format("Cloud not restore packages defined in '{0}'. {1}", allFiles, ex));
                return ExitCode.Failure;
            }

            try
            {
                foreach (var fileInfo in solutionFiles)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var packagesDirectory = Path.Combine(fileInfo.Directory.FullName, "packages");

                    if (Directory.Exists(packagesDirectory))
                    {
                        foreach (var nuGetPackageRestoreFix in _fixes)
                        {
                           await nuGetPackageRestoreFix.FixAsync(packagesDirectory, logger);
                        } 
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                logger.WriteWarning(ex.ToString());
            }

            return ExitCode.Success;
        }
    }
}