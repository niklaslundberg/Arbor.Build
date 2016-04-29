using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.ProcessUtils;

using JetBrains.Annotations;

using NuGet;

using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(800)]
    [UsedImplicitly]
    public class NuGetPackageUploader : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_Enabled, defaultValue: false);
            bool websitePackagesUploadEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_WebSitePackagesUploadEnabled, defaultValue: false);

            if (!enabled)
            {
                logger.Write("NuGet package upload is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value, "packages"));
            var websitesDirectory = new DirectoryInfo(Path.Combine(artifacts.Value, "websites"));

            var nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue();
            var nugetServer =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGetServer_Uri, "");
            var nuGetServerApiKey =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGetServer_ApiKey, "");

            var isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(defaultValue: false);
            bool forceUpload = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled, defaultValue: false);

            int timeoutInSeconds = buildVariables.GetInt32ByKey(WellKnownVariables.ExternalTools_NuGetServer_UploadTimeoutInSeconds, defaultValue: -1);

            bool checkNuGetPackagesExists = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_CheckPackageExists, defaultValue: false);
            string sourceName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGetServer_SourceName, defaultValue: "");

            if (isRunningOnBuildAgent)
            {
                logger.Write("NuGet package upload is enabled");
            }
            if (!isRunningOnBuildAgent && forceUpload)
            {
                logger.Write(
                    $"NuGet package upload is enabled by the flag '{WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled}'");
            }

            if (isRunningOnBuildAgent || forceUpload)
            {
                return UploadNuGetPackagesAsync(logger, packagesFolder, nugetExe.Value, nugetServer,
                    nuGetServerApiKey, websitePackagesUploadEnabled, websitesDirectory, timeoutInSeconds, checkNuGetPackagesExists, sourceName);
            }

            logger.Write(
                $"Not running on build server. Skipped package upload. Set environment variable '{WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload");

            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> UploadNuGetPackagesAsync(ILogger logger, DirectoryInfo artifactPackagesDirectory, string nugetExePath,
            string serverUri,
            string apiKey,
            bool websitePackagesUploadEnabled,
            DirectoryInfo websitesDirectory,
            int timeoutInseconds,
            bool checkNuGetPackagesExists,
            string sourceName)
        {
            if (artifactPackagesDirectory == null)
            {
                throw new ArgumentNullException(nameof(artifactPackagesDirectory));
            }

            if (string.IsNullOrWhiteSpace(nugetExePath))
            {
                throw new ArgumentNullException(nameof(nugetExePath));
            }

            if (websitesDirectory == null)
            {
                throw new ArgumentNullException(nameof(websitesDirectory));
            }


            var nuGetPackageFiles = new List<FileInfo>();

            if (!artifactPackagesDirectory.Exists)
            {
                logger.WriteWarning("There is no packages folder, skipping standard package upload");
            }
            else
            {
                var standardPackages =
                    artifactPackagesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                        .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) < 0)
                        .ToList();

                nuGetPackageFiles.AddRange(standardPackages);
            }

            if (!websitePackagesUploadEnabled)
            {
                logger.Write("Website package upload is disabled");

            }
            else if (!websitesDirectory.Exists)
            {
                logger.WriteWarning("There is no website package folder, skipping website package upload");
            }
            else
            {
                var websitePackages =
                    websitesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                        .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) < 0)
                        .ToList();

                nuGetPackageFiles.AddRange(websitePackages);
            }

            if (!nuGetPackageFiles.Any())
            {
                var websiteUploadMissingMessage = websitePackagesUploadEnabled
                                                      ? $" or in folder websites folder '{websitesDirectory.FullName}'"
                                                      : "";

                logger.Write(
                    $"Could not find any NuGet packages to upload in folder '{artifactPackagesDirectory}' or any subfolder {websiteUploadMissingMessage}");

                return ExitCode.Success;
            }

            logger.Write($"Found {nuGetPackageFiles.Count} NuGet packages to upload");

            bool result = true;

            var sortedPackages = nuGetPackageFiles
                .OrderByDescending(package => package.Name.Length)
                .SafeToReadOnlyCollection();

            if (checkNuGetPackagesExists)
            {
                logger.Write($"Checking if packages already exists in NuGet source");

                foreach (var fileInfo in sortedPackages)
                {
                    bool? packageExists = await CheckPackageExistsAsync(fileInfo, nugetExePath, serverUri, logger, sourceName);

                    if (!packageExists.HasValue)
                    {
                        logger.WriteError(
                            $"The NuGet package '{fileInfo.Name}' could not be determined if exists or not, skipping package push");
                        return ExitCode.Failure;
                    }

                    if (packageExists.Value)
                    {
                        logger.WriteError(
                            $"The NuGet package '{fileInfo.Name}' was found at the NuGet source, skipping package push");

                        return ExitCode.Failure;
                    }
                }
            }
            else
            {
                logger.Write($"Skipping checking if packages already exists in NuGet source");
            }

            foreach (var fileInfo in sortedPackages)
            {
                string nugetPackage = fileInfo.FullName;

                ExitCode exitCode = await UploadNugetPackageAsync(nugetExePath, serverUri, apiKey, nugetPackage, logger, timeoutInseconds, checkNuGetPackagesExists);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }

        private async Task<bool?> CheckPackageExistsAsync(FileInfo nugetPackage, string nugetExePath, string serverUri, ILogger logger, string sourceName)
        {
            if (!File.Exists(nugetPackage.FullName))
            {
                logger.WriteError(
                    $"The NuGet package '{nugetPackage}' does not exist");
                return null;
            }

            logger.WriteDebug($"Searching for existing NuGet package '{nugetPackage}'");

            var nugetZipPackage = new ZipPackage(nugetPackage.FullName);

            SemanticVersion expectedVersion = nugetZipPackage.Version;

            var args = new List<string>
                       {
                           "list",
                           nugetZipPackage.Id
                       };

            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                logger.WriteVerbose($"Using specific source name '{sourceName}'");
                args.Add("-source");
                args.Add(sourceName);
            }

            args.Add("-verbosity");
            args.Add("normal");

            if (global::NuGet.Versioning.SemanticVersion.Parse(nugetZipPackage.Version.ToNormalizedString()).IsPrerelease)
            {
                logger.WriteVerbose($"Package '{nugetPackage.Name}' is pre-release");
                args.Add("-prerelease");
            }

            StringBuilder errorBuilder = new StringBuilder();
            List<string> standardBuilder = new List<string>();


            var expectedNameAndVersion = $"{nugetZipPackage.Id} {expectedVersion.ToNormalizedString()}";

            logger.Write($"Looking for '{expectedNameAndVersion}' package");

            var exitCode =
                await
                ProcessRunner.ExecuteAsync(
                    nugetExePath,
                    arguments: args,
                    standardOutLog:
                    (message, prefix) =>
                        {
                            standardBuilder.Add(message);
                            logger.Write(message, prefix);
                        },
                    standardErrorAction: (message, prefix) =>
                        {
                            errorBuilder.AppendLine(message);
                            logger.WriteError(message, prefix);
                        },
                    toolAction: logger.Write,
                    addProcessNameAsLogCategory: true,
                    addProcessRunnerCategory: true);

            if (!exitCode.IsSuccess)
            {
                logger.WriteError($"Could not execute process to check if package '{expectedNameAndVersion}' exists");
                return null;
            }

            bool foundSpecificPackage = standardBuilder.Any(line => line.Equals(expectedNameAndVersion, StringComparison.InvariantCultureIgnoreCase));

            if (foundSpecificPackage)
            {
                logger.Write($"Found existing package id '{expectedNameAndVersion}'");
            }
            else
            {
                logger.Write($"Could not find existing package id '{expectedNameAndVersion}'");
            }

            return foundSpecificPackage;
        }

        static async Task<ExitCode> UploadNugetPackageAsync(string nugetExePath, string serverUri, string apiKey, string nugetPackage, ILogger logger, int timeoutInseconds, bool checkNuGetPackagesExists)
        {
            if (!File.Exists(nugetPackage))
            {
                logger.WriteError(
                    $"The NuGet package '{nugetPackage}' does not exist, when trying to push to nuget source");
                return ExitCode.Failure;
            }


            logger.WriteDebug($"Pushing NuGet package '{nugetPackage}'");

            var args = new List<string>
                       {
                           "push",
                           nugetPackage
                       };

            if (!string.IsNullOrWhiteSpace(serverUri))
            {
                args.Add("-source");
                args.Add(serverUri);
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                args.Add(apiKey);
            }

            args.Add("-verbosity");
            args.Add("detailed");

            if (timeoutInseconds > 0)
            {
                args.Add("-timeout");
                args.Add(timeoutInseconds.ToString(CultureInfo.InvariantCulture));
            }

            const int MaxAttempts = 5;

            ExitCode exitCode = ExitCode.Failure;


            int attemptCount = 1;
            while (!exitCode.IsSuccess && attemptCount <= MaxAttempts)
            {
                StringBuilder errorBuilder = new StringBuilder();

                exitCode =
                    await
                        ProcessRunner.ExecuteAsync(nugetExePath, arguments: args, standardOutLog: logger.Write,
                            standardErrorAction: (message, prefix) =>
                                {
                                    errorBuilder.AppendLine(message);
                                    logger.WriteError(message, prefix);
                                }, toolAction: logger.Write,
                            addProcessNameAsLogCategory: true,
                            addProcessRunnerCategory: true);

                if (!exitCode.IsSuccess
                    && errorBuilder.ToString().IndexOf("conflict", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    if (checkNuGetPackagesExists)
                    {
                        logger.WriteWarning(
                            $"The NuGet package could not be pushed, however, the pre-check if the package exists succeeded, so this error might be temporal");

                        return ExitCode.Success;
                    }

                    logger.WriteError($"Failed to upload NuGet package '{nugetPackage}', skipping retry for NuGet package, conflict detected");

                    return exitCode;
                }

                if (!exitCode.IsSuccess && attemptCount < MaxAttempts)
                {
                    logger.WriteWarning(
                        $"Failed to upload nuget package '{nugetPackage}', attempt {attemptCount} of {MaxAttempts}, retrying...");
                }

                attemptCount++;

                if (!exitCode.IsSuccess && attemptCount == MaxAttempts)
                {
                    logger.WriteError(
                        $"Failed to upload nuget package '{nugetPackage}' on last attempt {attemptCount} of {MaxAttempts}");
                }
            }

            return exitCode;
        }
    }
}
