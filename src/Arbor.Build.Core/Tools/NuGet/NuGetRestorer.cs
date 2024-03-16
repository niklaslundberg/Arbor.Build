using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(101)]
[UsedImplicitly]
public class NuGetRestorer(IFileSystem fileSystem, BuildContext buildContext) : ITool
{
    public async Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        bool enabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.NuGetRestoreEnabled);

        if (!enabled)
        {
            logger.Debug("{Tool} is disabled", nameof(NuGetRestorer));
            return ExitCode.Success;
        }

        var nugetExePath =
            buildVariables.GetVariable(WellKnownVariables.ExternalTools_NuGet_ExePath).GetValueOrThrow().ParseAsPath();

        FileEntry[] solutionFiles = buildContext.SourceRoot.GetFiles( "*.sln", SearchOption.AllDirectories).ToArray();

        PathLookupSpecification pathLookupSpecification =
            DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

        var excludeListStatus = solutionFiles
            .Select(file => new { File = file, Status = pathLookupSpecification.IsFileExcluded(file, buildContext.SourceRoot) })
            .ToArray();

        FileEntry[] included = excludeListStatus
            .Where(file => !file.Status.Item1)
            .Select(file => file.File)
            .ToArray();

        var excluded = excludeListStatus
            .Where(file => file.Status.Item1)
            .ToArray();

        if (included.Length > 1)
        {
            logger.Error("Expected exactly 1 solution file, found {Length}, {V}",
                included.Length,
                string.Join(", ", included.Select(s => s.FullName)));
            return ExitCode.Failure;
        }

        if (included.Length == 0)
        {
            logger.Error("Expected exactly 1 solution file, found 0");
            return ExitCode.Failure;
        }

        if (excluded.Length > 0)
        {
            logger.Warning("Found not allowed solution files: {Files}",
                string.Join(", ",
                    excluded.Select(excludedItem => $"{fileSystem.ConvertPathToInternal(excludedItem.File.Path)} ({excludedItem.Status.Item2})")));
        }

        var solutionFile = included.Single();

        var arguments = new List<string> { "restore", fileSystem.ConvertPathToInternal(solutionFile.Path) };

        ExitCode result = await ProcessHelper.ExecuteAsync(
            fileSystem.ConvertPathToInternal(nugetExePath),
            arguments,
            logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }
}