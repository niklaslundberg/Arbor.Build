using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Git;

[Priority(250)]
[UsedImplicitly]
public class GitSourceLinkMetadataProvider(IFileSystem fileSystem) : ITool
{
    private const string GitDirectoryName = ".git";

    public async Task<ExitCode> ExecuteAsync(ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        bool deterministicBuildEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.DeterministicBuildEnabled);

        if (!deterministicBuildEnabled)
        {
            return ExitCode.Success;
        }

        var sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).Value!.ParseAsPath();

        var sourceRootDirectory = new DirectoryEntry(fileSystem, sourceRoot);

        var dotGitDirectory = sourceRootDirectory.EnumerateDirectories(GitDirectoryName).SingleOrDefault();

        if (dotGitDirectory is {Exists: true})
        {
            return ExitCode.Success;
        }

        dotGitDirectory = new DirectoryEntry(fileSystem, UPath.Combine(sourceRootDirectory.FullName, GitDirectoryName));

        dotGitDirectory.EnsureExists();

        string gitHash = buildVariables.Require(WellKnownVariables.GitHash).Value!;
        string repositoryUrl = buildVariables.Require(WellKnownVariables.RepositoryUrl).Value!;

        var headFile = new FileEntry(fileSystem, UPath.Combine(dotGitDirectory.FullName, "HEAD"));
        var configFile = new FileEntry(fileSystem, UPath.Combine(dotGitDirectory.FullName, "config"));

        await headFile.FileSystem.WriteAllTextAsync(headFile.Path, gitHash, Encoding.UTF8, cancellationToken: cancellationToken);

        string origin = $@"[remote ""origin""]
{"\t"}url = {repositoryUrl}";

        await configFile.FileSystem.WriteAllTextAsync(configFile.Path, origin, Encoding.UTF8, cancellationToken: cancellationToken);

        return ExitCode.Success;
    }
}