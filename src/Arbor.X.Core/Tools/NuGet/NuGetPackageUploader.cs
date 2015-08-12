using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(800)]
    public class NuGetPackageUploader : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_Enabled, defaultValue: false);

            if (!enabled)
            {
                logger.Write("NuGet package upload is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value, "packages"));

            if (!packagesFolder.Exists)
            {
                logger.WriteWarning("There is no packages folder, skipping package upload");
                return Task.FromResult(ExitCode.Success);
            }

            var nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue();
            var nugetServer =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGetServer_Uri, "");
            var nuGetServerApiKey =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGetServer_ApiKey, "");

            var isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(defaultValue: false);
            bool forceUpload = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled, defaultValue: false);

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
                return UploadNuGetPackages(logger, packagesFolder.FullName, nugetExe.Value, nugetServer,
                    nuGetServerApiKey);
            }

            logger.Write(
                $"Not running on build server. Skipped package upload. Set environment variable '{WellKnownVariables.ExternalTools_NuGetServer_ForceUploadEnabled}' to value 'true' to force package upload");

            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> UploadNuGetPackages(ILogger logger, string artifactPackagesFolder, string nugetExePath,
            string serverUri,
            string apiKey)
        {
            if (string.IsNullOrWhiteSpace(artifactPackagesFolder))
            {
                throw new ArgumentNullException(nameof(artifactPackagesFolder));
            }

            if (string.IsNullOrWhiteSpace(nugetExePath))
            {
                throw new ArgumentNullException(nameof(nugetExePath));
            }

            var files = new DirectoryInfo(artifactPackagesFolder)
                .EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) < 0)
                .ToList();

            if (!files.Any())
            {
                logger.Write(
                    $"Could not find any NuGet packages to upload in folder '{artifactPackagesFolder}' or any subfolder");

                return ExitCode.Success;
            }

            logger.Write($"Found {files.Count} NuGet packages to upload");

            bool result = true;

            foreach (var fileInfo in files)
            {
                string nugetPackage = fileInfo.FullName;

                var exitCode = await UploadNugetPackageAsync(nugetExePath, serverUri, apiKey, nugetPackage, logger);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }

        static async Task<ExitCode> UploadNugetPackageAsync(string nugetExePath, string serverUri, string apiKey,
            string nugetPackage, ILogger logger)
        {
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

            var exitCode =
                await
                    ProcessRunner.ExecuteAsync(nugetExePath, arguments: args, standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write);

            return exitCode;
        }
    }
}