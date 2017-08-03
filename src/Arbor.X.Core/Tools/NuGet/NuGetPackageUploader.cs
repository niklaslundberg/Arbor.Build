using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(800)]
    [UsedImplicitly]
    public class NuGetPackageUploader : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_Enabled, false);
            bool websitePackagesUploadEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_NuGetServer_WebSitePackagesUploadEnabled,
                    false);

            if (!enabled)
            {
                logger.Write("NuGet package upload is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value, "packages"));
            var websitesDirectory = new DirectoryInfo(Path.Combine(artifacts.Value, "websites"));

            IVariable nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
                .ThrowIfEmptyValue();
            string nugetServer =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_Uri,
                    string.Empty);
            string nuGetServerApiKey =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_ApiKey,
                    string.Empty);

            IVariable isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(false);
            bool forceUpload =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled, false);

            bool timeoutIncreaseEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_UploadTimeoutIncreaseEnabled, false);

            int timeoutInSeconds =
                buildVariables.GetInt32ByKey(WellKnownVariables.ExternalTools_NuGetServer_UploadTimeoutInSeconds, -1);

            bool checkNuGetPackagesExists =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_CheckPackageExists, false);
            string sourceName =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_SourceName,
                    string.Empty);

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
                return UploadNuGetPackagesAsync(
                    logger,
                    packagesFolder,
                    nugetExe.Value,
                    nugetServer,
                    nuGetServerApiKey,
                    websitePackagesUploadEnabled,
                    websitesDirectory,
                    timeoutInSeconds,
                    checkNuGetPackagesExists,
                    sourceName,
                    timeoutIncreaseEnabled);
            }

            logger.Write(
                $"Not running on build server. Skipped package upload. Set environment variable '{WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload");

            return Task.FromResult(ExitCode.Success);
        }

        private static async Task<ExitCode> UploadNugetPackageAsync(
            string nugetExePath,
            string serverUri,
            string apiKey,
            string nugetPackage,
            ILogger logger,
            int timeoutInseconds,
            bool checkNuGetPackagesExists,
            bool timeoutIncreaseEnabled)
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

            const int MaxAttempts = 5;

            ExitCode exitCode = ExitCode.Failure;

            int attemptCount = 1;
            while (!exitCode.IsSuccess && attemptCount <= MaxAttempts)
            {
                var errorBuilder = new StringBuilder();

                List<string> runSpecificArgs = args.ToList();

                if (timeoutInseconds > 0)
                {
                    runSpecificArgs.Add("-timeout");

                    int timeout;

                    if (timeoutIncreaseEnabled)
                    {
                        timeout = timeoutInseconds * attemptCount;
                    }
                    else
                    {
                        timeout = timeoutInseconds;
                    }

                    runSpecificArgs.Add(timeout.ToString(CultureInfo.InvariantCulture));
                }

                exitCode =
                    await
                        ProcessRunner.ExecuteAsync(
                            nugetExePath,
                            arguments: runSpecificArgs,
                            standardOutLog: logger.Write,
                            standardErrorAction: (message, prefix) =>
                            {
                                errorBuilder.AppendLine(message);
                                logger.WriteError(message, prefix);
                            },
                            toolAction: logger.Write,
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

                    logger.WriteError(
                        $"Failed to upload NuGet package '{nugetPackage}', skipping retry for NuGet package, conflict detected");

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

        private async Task<ExitCode> UploadNuGetPackagesAsync(
            ILogger logger,
            DirectoryInfo artifactPackagesDirectory,
            string nugetExePath,
            string serverUri,
            string apiKey,
            bool websitePackagesUploadEnabled,
            DirectoryInfo websitesDirectory,
            int timeoutInseconds,
            bool checkNuGetPackagesExists,
            string sourceName,
            bool timeoutIncreaseEnabled)
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
                List<FileInfo> standardPackages =
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
                List<FileInfo> websitePackages =
                    websitesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                        .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) < 0)
                        .ToList();

                nuGetPackageFiles.AddRange(websitePackages);
            }

            if (!nuGetPackageFiles.Any())
            {
                string websiteUploadMissingMessage = websitePackagesUploadEnabled
                    ? $" or in folder websites folder '{websitesDirectory.FullName}'"
                    : string.Empty;

                logger.Write(
                    $"Could not find any NuGet packages to upload in folder '{artifactPackagesDirectory}' or any subfolder {websiteUploadMissingMessage}");

                return ExitCode.Success;
            }

            string files =
                string.Join(Environment.NewLine, nuGetPackageFiles.Select(
                    file => $"{file.FullName}: {file.Length / 1024.0:F1} KiB"));

            logger.Write($"Found {nuGetPackageFiles.Count} NuGet packages to upload {files}");

            bool result = true;

            IReadOnlyCollection<FileInfo> sortedPackages = nuGetPackageFiles
                .OrderByDescending(package => package.Name.Length)
                .SafeToReadOnlyCollection();

            if (checkNuGetPackagesExists)
            {
                logger.Write($"Checking if packages already exists in NuGet source");

                foreach (FileInfo fileInfo in sortedPackages)
                {
                    bool? packageExists =
                        await CheckPackageExistsAsync(fileInfo, nugetExePath, logger, sourceName);

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

            foreach (FileInfo fileInfo in sortedPackages)
            {
                string nugetPackage = fileInfo.FullName;

                ExitCode exitCode = await UploadNugetPackageAsync(
                    nugetExePath,
                    serverUri,
                    apiKey,
                    nugetPackage,
                    logger,
                    timeoutInseconds,
                    checkNuGetPackagesExists,
                    timeoutIncreaseEnabled);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }

        private async Task<bool?> CheckPackageExistsAsync(
            FileInfo nugetPackage,
            string nugetExePath,
            ILogger logger,
            string sourceName)
        {
            if (!File.Exists(nugetPackage.FullName))
            {
                logger.WriteError(
                    $"The NuGet package '{nugetPackage}' does not exist");
                return null;
            }

            logger.WriteDebug($"Searching for existing NuGet package '{nugetPackage}'");

            string packageVersion;
            string packageId;

            using (var fs = new FileStream(nugetPackage.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var archive = new ZipArchive(fs))
                {
                    ZipArchiveEntry nuspecEntry =
                        archive.Entries.SingleOrDefault(
                            entry =>
                                Path.GetExtension(entry.Name)
                                    .Equals(".nuspec", StringComparison.InvariantCultureIgnoreCase));

                    if (nuspecEntry == null)
                    {
                        throw new InvalidOperationException("The nuget package does not contain any nuspec");
                    }

                    var nuspecReader = new NuspecReader(nuspecEntry.Open());
                    NuGetVersion nuGetVersion = nuspecReader.GetVersion();

                    packageVersion = nuGetVersion.ToNormalizedString();
                    packageId = nuspecReader.GetIdentity().Id;
                }
            }

            SemanticVersion expectedVersion = SemanticVersion.Parse(packageVersion);

            var packageInfo = new { Id = packageId, Version = expectedVersion };

            var args = new List<string>
            {
                "list",
                packageId
            };

            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                logger.WriteVerbose($"Using specific source name '{sourceName}'");
                args.Add("-source");
                args.Add(sourceName);
            }

            args.Add("-verbosity");
            args.Add("normal");

            if (packageInfo.Version.IsPrerelease)
            {
                logger.WriteVerbose($"Package '{nugetPackage.Name}' is pre-release");
                args.Add("-prerelease");
            }

            var errorBuilder = new StringBuilder();
            var standardBuilder = new List<string>();

            string expectedNameAndVersion = $"{packageInfo.Id} {expectedVersion.ToNormalizedString()}";

            logger.Write($"Looking for '{expectedNameAndVersion}' package");

            ExitCode exitCode =
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

            bool foundSpecificPackage = standardBuilder.Any(
                line => line.Equals(expectedNameAndVersion, StringComparison.InvariantCultureIgnoreCase));

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
    }
}
