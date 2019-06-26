using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.Symbols
{
    [Priority(800)]
    [UsedImplicitly]
    public class NuGetSymbolPackageUploader : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            logger = logger ?? Logger.None;

            bool enabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.ExternalTools_SymbolServer_Enabled);

            if (!enabled)
            {
                logger.Information("Symbol package upload is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value, "packages"));

            if (!packagesFolder.Exists)
            {
                logger.Warning("There is no packages folder, skipping package upload");
                return Task.FromResult(ExitCode.Success);
            }

            IVariable nugetExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue();
            IVariable symbolServer =
                buildVariables.Require(WellKnownVariables.ExternalTools_SymbolServer_Uri).ThrowIfEmptyValue();
            IVariable symbolServerApiKey =
                buildVariables.Require(WellKnownVariables.ExternalTools_SymbolServer_ApiKey).ThrowIfEmptyValue();

            IVariable isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(false);
            bool forceUpload =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_SymbolServer_ForceUploadEnabled);

            int timeout =
                buildVariables.GetInt32ByKey(
                    WellKnownVariables.ExternalTools_SymbolServer_UploadTimeoutInSeconds,
                    -1);

            if (isRunningOnBuildAgent)
            {
                logger.Information("Symbol package upload is enabled");
            }

            if (!isRunningOnBuildAgent && forceUpload)
            {
                logger.Information(
                    "Symbol package upload is enabled by the flag '{ExternalTools_SymbolServer_ForceUploadEnabled}'",
                    WellKnownVariables.ExternalTools_SymbolServer_ForceUploadEnabled);
            }

            if (isRunningOnBuildAgent || forceUpload)
            {
                return UploadNuGetPackagesAsync(
                    logger,
                    packagesFolder.FullName,
                    nugetExe.Value,
                    symbolServer.Value,
                    symbolServerApiKey.Value,
                    timeout);
            }

            logger.Information("Not running on build server. Skipped package upload");

            return Task.FromResult(ExitCode.Success);
        }

        private static async Task<ExitCode> UploadNugetPackageAsync(
            string nugetExePath,
            string symbolServerUrl,
            string apiKey,
            string nugetPackage,
            ILogger logger,
            int timeout)
        {
            var args = new List<string>
            {
                "push",
                nugetPackage,
                "-source",
                symbolServerUrl,
                apiKey,
                "-verbosity",
                "detailed"
            };

            if (timeout > 0)
            {
                args.Add("-timeout");
                args.Add(timeout.ToString(CultureInfo.InvariantCulture));
            }

            ExitCode exitCode =
                await
                    ProcessRunner.ExecuteProcessAsync(
                        nugetExePath,
                        arguments: args,
                        standardOutLog: logger.Information,
                        standardErrorAction: logger.Error,
                        toolAction: logger.Information).ConfigureAwait(false);

            return exitCode;
        }

        private async Task<ExitCode> UploadNuGetPackagesAsync(
            ILogger logger,
            string packagesFolder,
            string nugetExePath,
            string symbolServerUrl,
            string apiKey,
            int timeout)
        {
            if (string.IsNullOrWhiteSpace(packagesFolder))
            {
                throw new ArgumentNullException(nameof(packagesFolder));
            }

            if (string.IsNullOrWhiteSpace(nugetExePath))
            {
                throw new ArgumentNullException(nameof(nugetExePath));
            }

            if (string.IsNullOrWhiteSpace(symbolServerUrl))
            {
                throw new ArgumentNullException(nameof(symbolServerUrl));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            List<FileInfo> files = new DirectoryInfo(packagesFolder)
                .EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                .Where(file => file.Name.IndexOf("symbols", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            bool result = true;

            foreach (FileInfo fileInfo in files)
            {
                string nugetPackage = fileInfo.FullName;

                ExitCode exitCode =
                    await UploadNugetPackageAsync(nugetExePath, symbolServerUrl, apiKey, nugetPackage, logger, timeout)
                        .ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }
    }
}
