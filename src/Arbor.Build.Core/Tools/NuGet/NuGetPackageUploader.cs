using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Packaging;
using NuGet.Versioning;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(850)]
    [UsedImplicitly]
    public class NuGetPackageUploader : ITool
    {
        private static async Task<ExitCode> UploadNugetPackageAsync(
            string nugetExePath,
            string? serverUri,
            string? apiKey,
            string nugetPackage,
            ILogger logger,
            int timeoutInSeconds,
            bool checkNuGetPackagesExists,
            bool timeoutIncreaseEnabled,
            string? sourceName,
            string? configFile)
        {
            if (!File.Exists(nugetPackage))
            {
                logger.Error("The NuGet package '{NugetPackage}' does not exist, when trying to push to nuget source",
                    nugetPackage);
                return ExitCode.Failure;
            }

            logger.Debug("Pushing NuGet package '{NugetPackage}'", nugetPackage);

            var args = new List<string> { "push", nugetPackage };

            if (!string.IsNullOrWhiteSpace(configFile))
            {
                args.Add("-ConfigFile");
                args.Add(configFile);
            }

            if (!string.IsNullOrWhiteSpace(serverUri))
            {
                args.Add("-source");
                args.Add(serverUri);
            }
            else if (!string.IsNullOrWhiteSpace(sourceName))
            {
                args.Add("-source");
                args.Add(sourceName);
            }

            const string apiEnvironmentVariableName = "NuGetPushApiKey";

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                args.Add($"%{apiEnvironmentVariableName}%");
            }

            args.Add("-verbosity");
            args.Add("detailed");

            const int MaxAttempts = 5;

            ExitCode exitCode = ExitCode.Failure;

            int attemptCount = 1;
            while (!exitCode.IsSuccess && attemptCount <= MaxAttempts)
            {
                var errorBuilder = new StringBuilder();

                var runSpecificArgs = args.ToList();

                if (timeoutInSeconds > 0)
                {
                    runSpecificArgs.Add("-timeout");

                    int timeout;

                    if (timeoutIncreaseEnabled)
                    {
                        timeout = timeoutInSeconds * attemptCount;
                    }
                    else
                    {
                        timeout = timeoutInSeconds;
                    }

                    runSpecificArgs.Add(timeout.ToString(CultureInfo.InvariantCulture));
                }

                var environmentVariables = new Dictionary<string, string>();

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    environmentVariables.Add(apiEnvironmentVariableName, apiKey);
                }

                exitCode =
                    await
                        ProcessRunner.ExecuteProcessAsync(
                            nugetExePath,
                            runSpecificArgs,
                            logger.Information,
                            (message, prefix) =>
                            {
                                errorBuilder.AppendLine(message);
                                logger.Error("{Prefix} {Message}", prefix, message);
                            },
                            logger.Information,
                            environmentVariables: environmentVariables).ConfigureAwait(false);

                if (!exitCode.IsSuccess
                    && errorBuilder.ToString().Contains("conflict", StringComparison.OrdinalIgnoreCase))
                {
                    if (checkNuGetPackagesExists)
                    {
                        logger.Warning(
                            "The NuGet package could not be pushed, however, the pre-check if the package exists succeeded, so this error might be temporal");

                        return ExitCode.Success;
                    }

                    logger.Error(
                        "Failed to upload NuGet package '{NugetPackage}', skipping retry for NuGet package, conflict detected",
                        nugetPackage);

                    return exitCode;
                }

                if (!exitCode.IsSuccess && attemptCount < MaxAttempts)
                {
                    logger.Warning(
                        "Failed to upload nuget package '{NugetPackage}', attempt {AttemptCount} of {MaxAttempts}, retrying...",
                        nugetPackage,
                        attemptCount,
                        MaxAttempts);
                }

                attemptCount++;

                if (!exitCode.IsSuccess && attemptCount == MaxAttempts)
                {
                    logger.Error(
                        "Failed to upload nuget package '{NugetPackage}' on last attempt {AttemptCount} of {MaxAttempts}",
                        nugetPackage,
                        attemptCount,
                        MaxAttempts);
                }
            }

            return exitCode;
        }

        private async Task<ExitCode> UploadNuGetPackagesAsync(
            ILogger logger,
            DirectoryInfo artifactPackagesDirectory,
            string nugetExePath,
            string? serverUri,
            string? apiKey,
            bool websitePackagesUploadEnabled,
            DirectoryInfo websitesDirectory,
            int timeoutInSeconds,
            bool checkNuGetPackagesExists,
            string? sourceName,
            string? configFile,
            bool timeoutIncreaseEnabled,
            ImmutableArray<string> packagePatterns)
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
                logger.Warning("There is no packages folder, skipping standard package upload");
            }
            else
            {
                var allStandardPackages = new List<FileInfo>();

                if (packagePatterns.Length == 0)
                {
                    allStandardPackages.AddRange(artifactPackagesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories));
                }
                else
                {
                    foreach (string packagePattern in packagePatterns)
                    {
                        allStandardPackages.AddRange(artifactPackagesDirectory.EnumerateFiles(packagePattern, SearchOption.AllDirectories));
                    }
                }

                List<FileInfo> standardPackages = allStandardPackages
                    .Where(file => file.Name.IndexOf("symbols", StringComparison.OrdinalIgnoreCase) < 0)
                    .ToList();

                nuGetPackageFiles.AddRange(standardPackages);
            }

            if (!websitePackagesUploadEnabled)
            {
                logger.Information("Website package upload is disabled");
            }
            else if (!websitesDirectory.Exists)
            {
                logger.Warning("There is no website package folder, skipping website package upload");
            }
            else
            {
                var allWebSitePackages = new List<FileInfo>();

                if (packagePatterns.Length == 0)
                {
                    allWebSitePackages.AddRange( websitesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories));
                }
                else
                {
                    foreach (string packagePattern in packagePatterns)
                    {
                        allWebSitePackages.AddRange(websitesDirectory.EnumerateFiles(packagePattern, SearchOption.AllDirectories));
                    }
                }

                List<FileInfo> websitePackages = allWebSitePackages
                    .Where(file => file.Name.IndexOf("symbols", StringComparison.OrdinalIgnoreCase) < 0)
                    .ToList();

                nuGetPackageFiles.AddRange(websitePackages);
            }

            if (nuGetPackageFiles.Count == 0)
            {
                string websiteUploadMissingMessage = websitePackagesUploadEnabled
                    ? $" or in folder websites folder '{websitesDirectory.FullName}'"
                    : string.Empty;

                logger.Information(
                    "Could not find any NuGet packages to upload in folder '{ArtifactPackagesDirectory}' or any subfolder {WebsiteUploadMissingMessage}",
                    artifactPackagesDirectory,
                    websiteUploadMissingMessage);

                return ExitCode.Success;
            }

            string files =
                string.Join(Environment.NewLine,
                    nuGetPackageFiles.Select(
                        file => $"{file.FullName}: {file.Length / 1024.0:F1} KiB"));

            logger.Information("Found {Count} NuGet packages to upload {Files}", nuGetPackageFiles.Count, files);

            bool result = true;

            IReadOnlyCollection<FileInfo> sortedPackages = nuGetPackageFiles
                .OrderByDescending(package => package.Name.Length)
                .SafeToReadOnlyCollection();

            if (checkNuGetPackagesExists)
            {
                logger.Information("Checking if packages already exists in NuGet source");

                foreach (FileInfo fileInfo in sortedPackages)
                {
                    bool? packageExists =
                        await CheckPackageExistsAsync(fileInfo, nugetExePath, logger, sourceName).ConfigureAwait(false);

                    if (!packageExists.HasValue)
                    {
                        logger.Error(
                            "The NuGet package '{Name}' could not be determined if exists or not, skipping package push",
                            fileInfo.Name);
                        return ExitCode.Failure;
                    }

                    if (packageExists.Value)
                    {
                        logger.Error("The NuGet package '{Name}' was found at the NuGet source, skipping package push",
                            fileInfo.Name);

                        return ExitCode.Failure;
                    }
                }
            }
            else
            {
                logger.Information("Skipping checking if packages already exists in NuGet source");
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
                    timeoutInSeconds,
                    checkNuGetPackagesExists,
                    timeoutIncreaseEnabled,
                    sourceName,
                    configFile).ConfigureAwait(false);

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
            string? sourceName)
        {
            if (!File.Exists(nugetPackage.FullName))
            {
                logger.Error("The NuGet package '{NugetPackage}' does not exist", nugetPackage);
                return null;
            }

            logger.Debug("Searching for existing NuGet package '{NugetPackage}'", nugetPackage);

            string packageVersion;
            string packageId;

            using (var fs = new FileStream(nugetPackage.FullName, FileMode.Open, FileAccess.Read))
            {
                using var archive = new ZipArchive(fs);
                ZipArchiveEntry nuspecEntry =
                    archive.Entries.SingleOrDefault(entry =>
                        Path.GetExtension(entry.Name)
                            .Equals(".nuspec", StringComparison.OrdinalIgnoreCase));

                if (nuspecEntry == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture,
                            Resources.TheNuGetPackageIsMissingANuSpec,
                            nugetPackage.FullName));
                }

                var nuspecReader = new NuspecReader(nuspecEntry.Open());
                NuGetVersion nuGetVersion = nuspecReader.GetVersion();

                packageVersion = nuGetVersion.ToNormalizedString();
                packageId = nuspecReader.GetIdentity().Id;
            }

            SemanticVersion expectedVersion = SemanticVersion.Parse(packageVersion);

            var packageInfo = new { Id = packageId, Version = expectedVersion };

            var args = new List<string> { "list", $"packageid:{packageId}" };

            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                logger.Verbose("Using specific source name '{SourceName}'", sourceName);
                args.Add("-source");
                args.Add(sourceName);
            }

            args.Add("-verbosity");
            args.Add("normal");

            if (packageInfo.Version.IsPrerelease)
            {
                logger.Verbose("Package '{Name}' is pre-release", nugetPackage.Name);
                args.Add("-prerelease");
            }

            var errorBuilder = new StringBuilder();
            var standardBuilder = new List<string>();

            string expectedNameAndVersion = $"{packageInfo.Id} {expectedVersion.ToNormalizedString()}";

            logger.Information("Looking for '{ExpectedNameAndVersion}' package", expectedNameAndVersion);

            ExitCode exitCode =
                await
                    ProcessRunner.ExecuteProcessAsync(
                        nugetExePath,
                        args,
                        (message, prefix) =>
                        {
                            standardBuilder.Add(message);
                            logger.Information("{Prefix} {Message}", prefix, message);
                        },
                        (message, prefix) =>
                        {
                            errorBuilder.AppendLine(message);
                            logger.Error("{Prefix} {Message}", prefix, message);
                        },
                        logger.Information).ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                logger.Error("Could not execute process to check if package '{ExpectedNameAndVersion}' exists",
                    expectedNameAndVersion);
                return null;
            }

            bool foundSpecificPackage = standardBuilder.Any(
                line => line.Equals(expectedNameAndVersion, StringComparison.OrdinalIgnoreCase));

            if (foundSpecificPackage)
            {
                logger.Information("Found existing package id '{ExpectedNameAndVersion}'", expectedNameAndVersion);
            }
            else
            {
                logger.Information("Could not find existing package id '{ExpectedNameAndVersion}'",
                    expectedNameAndVersion);
            }

            return foundSpecificPackage;
        }

        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            logger ??= Logger.None ?? throw new ArgumentNullException(nameof(logger));
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_Enabled);
            bool websitePackagesUploadEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_NuGetServer_WebSitePackagesUploadEnabled);

            if (!enabled)
            {
                logger.Information("NuGet package upload is disabled ('{ExternalTools_NuGetServer_Enabled}')",
                    WellKnownVariables.ExternalTools_NuGetServer_Enabled);
                return Task.FromResult(ExitCode.Success);
            }

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value!, "packages"));
            var websitesDirectory = new DirectoryInfo(Path.Combine(artifacts.Value!, "websites"));

            IVariable nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
                .ThrowIfEmptyValue();

            string? nugetServer =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_Uri,
                    string.Empty);

            string? nuGetServerApiKey =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_ApiKey,
                    string.Empty);

            IVariable isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(false);
            bool forceUpload =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled);

            bool uploadOnFeatureBranches =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_UploadFeatureBranchEnabled);

            bool timeoutIncreaseEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_NuGetServer_UploadTimeoutIncreaseEnabled);

            int timeoutInSeconds =
                buildVariables.GetInt32ByKey(WellKnownVariables.ExternalTools_NuGetServer_UploadTimeoutInSeconds, -1);

            bool checkNuGetPackagesExists =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_CheckPackageExists);

            string? sourceName =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_SourceName,
                    string.Empty);

            string? configFile =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGetServer_ConfigFile,
                    string.Empty);

            string patterns =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables
                    .ExternalTools_NuGetServer_UploadPackagePatterns, "") ?? "";

            var packagePatterns = patterns.Split(';', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

            if (isRunningOnBuildAgent)
            {
                logger.Information("NuGet package upload is enabled");
            }

            string branchName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName, "")!;

            if (new BranchName(branchName).IsFeatureBranch() && !uploadOnFeatureBranches)
            {
                logger.Information("Package upload is not enabled for feature branches");
            }

            if (!isRunningOnBuildAgent && forceUpload)
            {
                logger.Information(
                    "NuGet package upload is enabled by the flag '{ExternalTools_NuGetServer_ForceUploadEnabled}'",
                    WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled);
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
                    configFile,
                    timeoutIncreaseEnabled,
                    packagePatterns);
            }

            logger.Information(
                "Not running on build server. Skipped package upload. Set environment variable '{ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload",
                WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled);

            return Task.FromResult(ExitCode.Success);
        }
    }
}