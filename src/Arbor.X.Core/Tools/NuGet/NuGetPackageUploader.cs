using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

using JetBrains.Annotations;

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
                    nuGetServerApiKey, websitePackagesUploadEnabled, websitesDirectory, timeoutInSeconds);
            }

            logger.Write(
                $"Not running on build server. Skipped package upload. Set environment variable '{WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload");

            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> UploadNuGetPackagesAsync(ILogger logger, DirectoryInfo artifactPackagesDirectory, string nugetExePath,
            string serverUri,
            string apiKey, bool websitePackagesUploadEnabled, DirectoryInfo websitesDirectory, int timeoutInseconds)
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
                    $"Could not find any NuGet packages to upload in folder '{artifactPackagesDirectory}' or any subfolder{websiteUploadMissingMessage}");

                return ExitCode.Success;
            }

            logger.Write($"Found {nuGetPackageFiles.Count} NuGet packages to upload");

            bool result = true;

            foreach (var fileInfo in nuGetPackageFiles)
            {
                string nugetPackage = fileInfo.FullName;

                var exitCode = await UploadNugetPackageAsync(nugetExePath, serverUri, apiKey, nugetPackage, logger, timeoutInseconds);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }

        static async Task<ExitCode> UploadNugetPackageAsync(string nugetExePath, string serverUri, string apiKey, string nugetPackage, ILogger logger, int timeoutInseconds)
        {
            logger.WriteDebug($"Pushing NuGet package '{nugetPackage}'");

            var args = new List<string>
                       {
                           "push",
                           nugetPackage
                       };

            if (!string.IsNullOrWhiteSpace(serverUri))
            {
                args.Add("-s");
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

            const int maxAttempts = 5;

            ExitCode exitCode = ExitCode.Failure;

            int attemptCount = 1;
            while (!exitCode.IsSuccess && attemptCount <= maxAttempts)
            {
                exitCode =
                    await
                        ProcessRunner.ExecuteAsync(nugetExePath, arguments: args, standardOutLog: logger.Write,
                            standardErrorAction: logger.WriteError, toolAction: logger.Write,
                            addProcessNameAsLogCategory: true,
                            addProcessRunnerCategory: true);

                if (!exitCode.IsSuccess && attemptCount < maxAttempts)
                {
                    logger.WriteWarning(
                        $"Failed to upload nuget package '{nugetPackage}', attempt {attemptCount} of {maxAttempts}, retrying...");
                }
                
                attemptCount++;

                if (!exitCode.IsSuccess && attemptCount == maxAttempts)
                {
                    logger.WriteError(
                        $"Failed to upload nuget package '{nugetPackage}' on last attempt {attemptCount} of {maxAttempts}");
                }
            }

            return exitCode;
        }
    }
}
