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
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Tools.Git;
using Arbor.FS;
using Arbor.Processing;
using Arbor.Tooler;
using JetBrains.Annotations;
using NuGet.Packaging;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(850)]
[UsedImplicitly]
public class NuGetPackageUploader(IFileSystem fileSystem) : ITool
{
    public Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
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

        var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value!.ParseAsPath();

        var artifactsPath = fileSystem.GetDirectoryEntry(artifacts);

        var packagesFolder = new DirectoryEntry(fileSystem, UPath.Combine(artifactsPath.Path, "packages"));
        var websitesDirectory = new DirectoryEntry(fileSystem, UPath.Combine(artifactsPath.Path, "websites"));

        UPath? nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
            .ThrowIfEmptyValue().Value?.ParseAsPath();

        if (!nugetExe.HasValue)
        {
            throw new InvalidOperationException("NuGet.exe is not valid");
        }

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

        bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault();

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
                .NuGetServerUploadPackageExcludeStartsWithPatterns, "") ?? "";

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

        var filters = new PackageUploadFilter(patterns, fileSystem);

        if (isRunningOnBuildAgent || forceUpload)
        {
            return UploadNuGetPackagesAsync(
                logger,
                packagesFolder,
                fileSystem.GetFileEntry(nugetExe.Value),
                nugetServer,
                nuGetServerApiKey,
                websitePackagesUploadEnabled,
                websitesDirectory,
                timeoutInSeconds,
                checkNuGetPackagesExists,
                sourceName,
                configFile,
                timeoutIncreaseEnabled,
                filters);
        }

        logger.Information(
            "Not running on build server. Skipped package upload. Set environment variable '{ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload",
            WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled);

        return Task.FromResult(ExitCode.Success);
    }

    private static async Task<ExitCode> UploadNugetPackageAsync(
        FileEntry nugetExePath,
        string? serverUri,
        string? apiKey,
        FileEntry nugetPackage,
        ILogger logger,
        int timeoutInSeconds,
        bool checkNuGetPackagesExists,
        bool timeoutIncreaseEnabled,
        string? sourceName,
        string? configFile)
    {
        if (!nugetPackage.Exists)
        {
            logger.Error("The NuGet package '{NugetPackage}' does not exist, when trying to push to nuget source",
                nugetPackage);
            return ExitCode.Failure;
        }

        logger.Debug("Pushing NuGet package '{NugetPackage}'", nugetPackage.ConvertPathToInternal());

        var args = new List<string> {"push", nugetPackage.ConvertPathToInternal()};

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

        const int maxAttempts = 5;

        var exitCode = ExitCode.Failure;

        int attemptCount = 1;
        while (!exitCode.IsSuccess && attemptCount <= maxAttempts)
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
                        nugetExePath.ConvertPathToInternal(),
                        runSpecificArgs,
                        logger.Information,
                        (message, prefix) =>
                        {
                            errorBuilder.AppendLine(message);
                            logger.Error("{Prefix} {Message}", prefix, message);
                        },
                        logger.Information,
                        environmentVariables: environmentVariables);

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

            if (!exitCode.IsSuccess && attemptCount < maxAttempts)
            {
                logger.Warning(
                    "Failed to upload nuget package '{NugetPackage}', attempt {AttemptCount} of {MaxAttempts}, retrying...",
                    nugetPackage,
                    attemptCount,
                    maxAttempts);
            }

            attemptCount++;

            if (!exitCode.IsSuccess && attemptCount == maxAttempts)
            {
                logger.Error(
                    "Failed to upload nuget package '{NugetPackage}' on last attempt {AttemptCount} of {MaxAttempts}",
                    nugetPackage,
                    attemptCount,
                    maxAttempts);
            }
        }

        return exitCode;
    }

    private async Task<ExitCode> UploadNuGetPackagesAsync(
        ILogger logger,
        DirectoryEntry artifactPackagesDirectory,
        FileEntry nugetExePath,
        string? serverUri,
        string? apiKey,
        bool websitePackagesUploadEnabled,
        DirectoryEntry websitesDirectory,
        int timeoutInSeconds,
        bool checkNuGetPackagesExists,
        string? sourceName,
        string? configFile,
        bool timeoutIncreaseEnabled,
        PackageUploadFilter? filter = default)
    {
        filter ??= new PackageUploadFilter("", fileSystem);

        var nuGetPackageFiles = new List<FileEntry>();

        if (!artifactPackagesDirectory.Exists)
        {
            logger.Warning("There is no packages folder, skipping standard package upload");
        }
        else
        {
            var allStandardPackages = new List<FileEntry>();

            allStandardPackages.AddRange(!filter.Exclusions.Any()
                ? artifactPackagesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                : artifactPackagesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                    .Where(file => filter.UploadEnable(file.Path.GetName())));

            var standardPackages = allStandardPackages
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
            var allWebSitePackages = new List<FileEntry>();

            allWebSitePackages.AddRange(!filter.Exclusions.Any()
                ? websitesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                : websitesDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                    .Where(file => filter.UploadEnable(file.Path.GetName())));

            var websitePackages = allWebSitePackages
                .Where(file => file.Name.IndexOf("symbols", StringComparison.OrdinalIgnoreCase) < 0)
                .ToList();

            nuGetPackageFiles.AddRange(websitePackages);
        }

        if (nuGetPackageFiles.Count == 0)
        {
            string websiteUploadMissingMessage = websitePackagesUploadEnabled
                ? $" or in folder websites folder '{websitesDirectory.ConvertPathToInternal()}'"
                : string.Empty;

            logger.Information(
                "Could not find any NuGet packages to upload in folder '{ArtifactPackagesDirectory}' or any subfolder {WebsiteUploadMissingMessage}",
                artifactPackagesDirectory.ConvertPathToInternal(),
                websiteUploadMissingMessage);

            return ExitCode.Success;
        }

        string files =
            string.Join(Environment.NewLine,
                nuGetPackageFiles.Select(
                    file => $"{fileSystem.ConvertPathToInternal(file.Path)}: {file.Length / 1024.0:F1} KiB"));

        logger.Information("Found {Count} NuGet packages to upload {Files}", nuGetPackageFiles.Count, files);

        bool result = true;

        IReadOnlyCollection<FileEntry> sortedPackages = nuGetPackageFiles
            .OrderByDescending(package => package.Name.Length)
            .SafeToReadOnlyCollection();

        if (checkNuGetPackagesExists)
        {
            logger.Information("Checking if packages already exists in NuGet source");

            foreach (var nugetPackage in sortedPackages)
            {
                bool? packageExists =
                    await CheckPackageExistsAsync(nugetPackage, logger, sourceName)
                        ;

                if (!packageExists.HasValue)
                {
                    logger.Error(
                        "The NuGet package '{Name}' could not be determined if exists or not, skipping package push",
                        nugetPackage.Name);
                    return ExitCode.Failure;
                }

                if (packageExists.Value)
                {
                    logger.Error("The NuGet package '{Name}' was found at the NuGet source, skipping package push",
                        nugetPackage.Name);

                    return ExitCode.Failure;
                }
            }
        }
        else
        {
            logger.Information("Skipping checking if packages already exists in NuGet source");
        }

        var filtered = sortedPackages
            .Where(package => !package.Name.Contains("dependabot", StringComparison.OrdinalIgnoreCase)
                              && !package.Name.Contains("-refs-tags-"))
            .ToImmutableArray();

        foreach (var nugetPackage in filtered)
        {
            await VerifyPackage(nugetPackage);

            var exitCode = await UploadNugetPackageAsync(
                nugetExePath,
                serverUri,
                apiKey,
                nugetPackage,
                logger,
                timeoutInSeconds,
                checkNuGetPackagesExists,
                timeoutIncreaseEnabled,
                sourceName,
                configFile);

            if (!exitCode.IsSuccess)
            {
                result = false;
            }
        }

        return result ? ExitCode.Success : ExitCode.Failure;
    }

    private async Task VerifyPackage(FileEntry nugetPackage)
    {
        await using var fs = new FileStream(nugetPackage.ConvertPathToInternal(), FileMode.Open, FileAccess.Read);

        using var archive = new ZipArchive(fs);

        var tempDirectoryPath = Path.GetTempPath().ParseAsPath() / Guid.NewGuid().ToString();

        var tempDirectory = new DirectoryEntry(nugetPackage.FileSystem, tempDirectoryPath);

        tempDirectory.EnsureExists();

        archive.ExtractToDirectory(tempDirectory.ConvertPathToInternal());

        tempDirectory.DeleteIfExists();
    }

    private static async Task<bool?> CheckPackageExistsAsync(
        FileEntry nugetPackage,
        ILogger logger,
        string? sourceName)
    {
        if (!nugetPackage.Exists)
        {
            logger.Error("The NuGet package '{NugetPackage}' does not exist", nugetPackage);
            return null;
        }

        logger.Debug("Searching for existing NuGet package '{NugetPackage}'", nugetPackage.ConvertPathToInternal());

        string packageVersion;
        NuGetPackageId packageId;

        await using (var fs = new FileStream(nugetPackage.ConvertPathToInternal(), FileMode.Open, FileAccess.Read))
        {
            using var archive = new ZipArchive(fs);

            ZipArchiveEntry? nuspecEntry =
                archive.Entries.SingleOrDefault(entry =>
                    Path.GetExtension(entry.Name) is { } extension
                    && extension.Equals(".nuspec", StringComparison.OrdinalIgnoreCase));

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
            packageId = new NuGetPackageId(nuspecReader.GetIdentity().Id);
        }

        var expectedVersion = SemanticVersion.Parse(packageVersion);

        string expectedNameAndVersion = $"{packageId.PackageId} {expectedVersion.ToNormalizedString()}";

        logger.Information("Looking for '{ExpectedNameAndVersion}' package", expectedNameAndVersion);

        var nuGetPackageInstaller = new NuGetPackageInstaller(logger: logger);
        var allVersions = await nuGetPackageInstaller.GetAllVersionsAsync(packageId, nuGetSource: sourceName);

        bool foundSpecificPackage = allVersions.Contains(expectedVersion);

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
}