using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Castanea;
using Arbor.Defensive.Collections;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;
using ExceptionExtensions = Arbor.Exceptions.ExceptionExtensions;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(100)]
    [UsedImplicitly]
    public class NuGetRestorer : ITool
    {
        private readonly IReadOnlyCollection<INuGetPackageRestoreFix> _fixes;

        public NuGetRestorer(IEnumerable<INuGetPackageRestoreFix> fixes)
        {
            _fixes = fixes.SafeToReadOnlyCollection();
        }

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var app = new CastaneaApplication();

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            string nuGetExetPath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
                .ThrowIfEmptyValue()
                .Value;

            var rootDirectory = new DirectoryInfo(vcsRoot);

            int listFilesAttempt = 1;

            int listFilesMaxAttempts = 5;

            bool listFilesSucceeded = false;

            IReadOnlyCollection<string> packagesConfigFiles = new List<string>();
            IReadOnlyCollection<FileInfo> solutionFiles = new List<FileInfo>();

            while (listFilesAttempt <= listFilesMaxAttempts && !listFilesSucceeded)
            {
                try
                {
                    rootDirectory.Refresh();

                    packagesConfigFiles =
                        rootDirectory.EnumerateFiles("packages.config", SearchOption.AllDirectories)
                            .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, vcsRoot))
                            .Select(file => file.FullName)
                            .ToReadOnlyCollection();

                    rootDirectory.Refresh();

                    solutionFiles =
                        rootDirectory.EnumerateFiles("*.sln", SearchOption.AllDirectories)
                            .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, vcsRoot))
                            .ToReadOnlyCollection();

                    listFilesSucceeded = true;
                }
                catch (Exception ex)
                {
                    if (listFilesAttempt == listFilesMaxAttempts)
                    {
                        logger.WriteError($"Could not enumerable packages.config files or solutions files. {ex}");
                        return ExitCode.Failure;
                    }

                    logger.WriteWarning(
                        $"Attempt {listFilesAttempt} of {listFilesMaxAttempts} failed, retrying. {ex}");
                    listFilesAttempt++;
                }
            }

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
                logger.WriteError("Found more than one solution file, cannot determine package output directory");
                return ExitCode.Failure;
            }

            FileInfo solutionFile = solutionFiles.Single();

            string allFiles = string.Join(Environment.NewLine, packagesConfigFiles);
            try
            {
// ReSharper disable once PossibleNullReferenceException
                string outputDirectoryPath = Path.Combine(solutionFile.Directory.FullName, "packages");

                new DirectoryInfo(outputDirectoryPath).EnsureExists();

                bool disableParallelProcessing =
                    buildVariables.GetBooleanByKey(
                        WellKnownVariables.NuGetRestoreDisableParallelProcessing,
                        false);

                bool noCache = buildVariables.GetBooleanByKey(
                    WellKnownVariables.NuGetRestoreNoCache,
                    false);

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

                int restoredPackages = 0;

                int attempt = 1;

                int maxAttempts = 5;

                bool succeeded = false;

                while (attempt <= maxAttempts && !succeeded)
                {
                    try
                    {
                        restoredPackages = app.RestoreAllSolutionPackages(
                            nuGetConfig,
                            message => logger.Write(message, prefix),
                            message => logger.WriteError(message, prefix),
                            debugAction);

                        if (restoredPackages == 0)
                        {
                            logger.WriteWarning($"No packages was restored as defined in {allFiles}");
                            return ExitCode.Success;
                        }

                        succeeded = true;
                    }
                    catch (Exception ex)
                    {
                        if (attempt < maxAttempts)
                        {
                            logger.WriteWarning(
                                $"Attempt {attempt} of {maxAttempts}: could not restore NuGet packages, trying againg. {ex.Message}");
                        }
                        else
                        {
                            logger.WriteError($"Could not restore NuGet packages.{ex}");
                            return ExitCode.Failure;
                        }

                        attempt++;
                    }
                }

                logger.Write($"Restored {restoredPackages} package configurations defined in {allFiles}");
            }
            catch (Exception ex)
            {
                logger.WriteError($"Could not restore packages defined in '{allFiles}'. {ex}");
                return ExitCode.Failure;
            }

            try
            {
                foreach (FileInfo fileInfo in solutionFiles)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    string packagesDirectory = Path.Combine(fileInfo.Directory.FullName, "packages");

                    if (Directory.Exists(packagesDirectory))
                    {
                        foreach (INuGetPackageRestoreFix nuGetPackageRestoreFix in _fixes)
                        {
                            await nuGetPackageRestoreFix.FixAsync(packagesDirectory, logger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExceptionExtensions.IsFatal(ex))
                {
                    throw;
                }

                logger.WriteWarning(ex.ToString());
            }

            return ExitCode.Success;
        }
    }
}
