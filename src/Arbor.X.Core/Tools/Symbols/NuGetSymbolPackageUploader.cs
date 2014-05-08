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
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
            var nugetExe = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue();
            var symbolServer =
                buildVariables.Require(WellKnownVariables.ExternalTools_SymbolServer_Uri).ThrowIfEmptyValue();
            var symbolServerApiKey =
                buildVariables.Require(WellKnownVariables.ExternalTools_SymbolServer_ApiKey).ThrowIfEmptyValue();

            var isRunningOnBuildAgentVariable =
                buildVariables.Require(WellKnownVariables.IsRunningOnBuildAgent).ThrowIfEmptyValue();

            bool isRunningOnBuildAgent = isRunningOnBuildAgentVariable.GetValueOrDefault(defaultValue:false);

            if (isRunningOnBuildAgent)
            {
                return UploadNuGetPackages(logger, artifacts.Value, nugetExe.Value, symbolServer.Value,
                                           symbolServerApiKey.Value);
            }

            logger.Write("Not running on build server. Skipped package upload");

            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> UploadNuGetPackages(ILogger logger, string artifactsFolder, string nugetExePath,
                                                   string symbolServerUrl,
                                                   string apiKey)
        {
            if (string.IsNullOrWhiteSpace(artifactsFolder))
            {
                throw new ArgumentNullException("artifactsFolder");
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

            var files = new DirectoryInfo(artifactsFolder)
                .EnumerateFiles("*.nupkg")
                .Where(file => file.Name.IndexOf("symbols", StringComparison.InvariantCultureIgnoreCase) >= 0);

            bool result = true;

            foreach (var fileInfo in files)
            {
                string nugetPackage = fileInfo.FullName;

                var exitCode = await UploadNugetPackageAsync(nugetExePath, symbolServerUrl, apiKey, nugetPackage, logger);

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
                               "-verbosity detailed"
                           };
            var exitCode =
                await
                ProcessRunner.ExecuteAsync(nugetExePath, arguments: args, standardOutLog: logger.Write,
                                           standardErrorAction: logger.WriteError, toolAction: logger.Write);

            return exitCode;
        }
    }
}