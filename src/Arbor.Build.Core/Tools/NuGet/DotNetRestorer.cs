using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.ProcessUtils;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(101)]
[UsedImplicitly]
public class DotNetRestorer(IFileSystem fileSystem) : ITool
{
    public async Task<ExitCode> ExecuteAsync(
        ILogger? logger,
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

        DirectoryEntry rootPath = new(fileSystem, buildVariables.GetVariable(WellKnownVariables.SourceRoot).GetValueOrThrow());

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
        FileEntry[] solutionFiles = rootPath
            .EnumerateFiles("*.sln", SearchOption.AllDirectories)
            .Where(file => !pathLookupSpecification.IsFileExcluded(file, rootPath).Item1)
            .ToArray();

        string? runtimeIdentifier =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.ProjectMSBuildPublishRuntimeIdentifier);

        foreach (var solutionFile in solutionFiles)
        {
            var arguments = new List<string> { "restore", solutionFile.FullName };
            if (!string.IsNullOrWhiteSpace(runtimeIdentifier))
            {
                arguments.Add(runtimeIdentifier);
            }

            ExitCode result = await ProcessHelper.ExecuteAsync(
                fileSystem.ConvertPathToInternal(dotNetExePath.ParseAsPath()),
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