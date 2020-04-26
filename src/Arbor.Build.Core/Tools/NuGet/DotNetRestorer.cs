using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class DotNetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            logger ??= Logger.None;
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.DotNetRestoreEnabled);

            if (!enabled)
            {
                return ExitCode.Success;
            }

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).GetValueOrThrow();

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty)!;

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Information(
                    "Path to 'dotnet.exe' has not been specified, set variable '{DotNetExePath}' or ensure the dotnet.exe is installed in its standard location",
                    WellKnownVariables.DotNetExePath);
                return ExitCode.Failure;
            }

            var pathLookupSpecification = new PathLookupSpecification();
            FileInfo[] solutionFiles = new DirectoryInfo(rootPath)
                .GetFiles("*.sln", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileExcluded(file.FullName, rootPath).Item1)
                .ToArray();

            string? runtimeIdentifier =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ProjectMSBuildPublishRuntimeIdentifier);

            foreach (FileInfo solutionFile in solutionFiles)
            {
                var arguments = new List<string> { "restore", solutionFile.FullName };
                if (!string.IsNullOrWhiteSpace(runtimeIdentifier))
                {
                    arguments.Add(runtimeIdentifier);
                }

                ExitCode result = await ProcessHelper.ExecuteAsync(
                    dotNetExePath,
                    arguments,
                    logger,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }
    }
}
