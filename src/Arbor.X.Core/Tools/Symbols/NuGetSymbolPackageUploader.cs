using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

namespace Arbor.X.Core.Tools.Symbols
{
    [Priority(800)]
    public class NuGetSymbolPackageUploader : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_SymbolServer_Enabled,
                defaultValue: false);

            if (!enabled)
            {
                logger.Write("Symbol package upload is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();

            var packagesFolder = new DirectoryInfo(Path.Combine(artifacts.Value, "packages"));

            if (!packagesFolder.Exists)
            {
                logger.WriteWarning("There is no packages folder, skipping package upload");
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

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(defaultValue: false);
            bool forceUpload =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_SymbolServer_ForceUploadEnabled,
                    defaultValue: false);

            if (isRunningOnBuildAgent)
            {
                logger.Write("Symbol package upload is enabled");
            }
            if (!isRunningOnBuildAgent && forceUpload)
            {
                logger.Write(string.Format("Symbol package upload is enabled by the flag '{0}'",
                    WellKnownVariables.ExternalTools_SymbolServer_ForceUploadEnabled));
            }

            if (isRunningOnBuildAgent || forceUpload)
            {
                return UploadNuGetPackages(logger, packagesFolder.FullName, nugetExe.Value, symbolServer.Value,
                    symbolServerApiKey.Value);
            }

            logger.Write("Not running on build server. Skipped package upload");

            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> UploadNuGetPackages(ILogger logger, string packagesFolder, string nugetExePath,
            string symbolServerUrl,
            string apiKey)
        {
            if (string.IsNullOrWhiteSpace(packagesFolder))
            {
                throw new ArgumentNullException("packagesFolder");
            }
            if (string.IsNullOrWhiteSpace(nugetExePath))
            {
                throw new ArgumentNullException("nugetExePath");
            }
            if (string.IsNullOrWhiteSpace(symbolServerUrl))
            {
                throw new ArgumentNullException("symbolServerUrl");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException("apiKey");
            }

            List<FileInfo> files = new DirectoryInfo(packagesFolder)
                .EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) >= 0)
                .ToList();

            bool result = true;

            foreach (FileInfo fileInfo in files)
            {
                string nugetPackage = fileInfo.FullName;

                ExitCode exitCode =
                    await UploadNugetPackageAsync(nugetExePath, symbolServerUrl, apiKey, nugetPackage, logger);

                if (!exitCode.IsSuccess)
                {
                    result = false;
                }
            }

            return result ? ExitCode.Success : ExitCode.Failure;
        }

        static async Task<ExitCode> UploadNugetPackageAsync(string nugetExePath, string symbolServerUrl, string apiKey,
            string nugetPackage, ILogger logger)
        {
            var args = new List<string>
                       {
                           "push",
                           nugetPackage,
                           "-s",
                           symbolServerUrl,
                           apiKey,
                           "-verbosity",
                           "detailed"
                       };
            ExitCode exitCode =
                await
                    ProcessRunner.ExecuteAsync(nugetExePath, arguments: args, standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write);

            return exitCode;
        }
    }
}