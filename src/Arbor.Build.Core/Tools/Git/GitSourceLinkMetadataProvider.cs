using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Git
{
    [Priority(250)]
    [UsedImplicitly]
    public class GitSourceLinkMetadataProvider : ITool
    {
        private const string GitDirectoryName = ".git";
        private readonly IFileSystem _fileSystem;

        public GitSourceLinkMetadataProvider(IFileSystem fileSystem) => _fileSystem = fileSystem;

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

            var sourceRoot = _fileSystem.ConvertPathFromInternal(buildVariables.Require(WellKnownVariables.SourceRoot).Value!);

            var sourceRootDirectory = new DirectoryEntry(_fileSystem, sourceRoot);

            var dotGitDirectory = sourceRootDirectory.EnumerateDirectories(GitDirectoryName).SingleOrDefault();

            if (dotGitDirectory is {Exists: true})
            {
                return ExitCode.Success;
            }

            dotGitDirectory = new DirectoryEntry(_fileSystem, UPath.Combine(sourceRootDirectory.FullName, GitDirectoryName));

            dotGitDirectory.EnsureExists();

            string gitHash = buildVariables.Require(WellKnownVariables.GitHash).Value!;
            string repositoryUrl = buildVariables.Require(WellKnownVariables.RepositoryUrl).Value!;

            var headFile = new FileEntry(_fileSystem, UPath.Combine(dotGitDirectory.FullName, "HEAD"));
            var configFile = new FileEntry(_fileSystem, UPath.Combine(dotGitDirectory.FullName, "config"));

            await headFile.WriteAllTextAsync(gitHash, Encoding.UTF8, cancellationToken);

            string origin = $@"[remote ""origin""]
{"\t"}url = {repositoryUrl}";

            await configFile.WriteAllTextAsync(origin, Encoding.UTF8, cancellationToken);

            return ExitCode.Success;
        }
    }
}