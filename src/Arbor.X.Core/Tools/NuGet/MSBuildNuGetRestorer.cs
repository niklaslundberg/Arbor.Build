using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;

using Arbor.X.Core.ProcessUtils;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class MSBuildNuGetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.MSBuildNuGetRestoreEnabled,
                false);

            if (!enabled)
            {
                return ExitCode.Success;
            }

            string msbuildExePath =
                buildVariables.GetVariable(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string[] solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            PathLookupSpecification pathLookupSpecification =
                DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

            var blackListStatus = solutionFiles
                .Select(file => new { File = file, Status = pathLookupSpecification.IsFileBlackListed(file, rootPath) })
                .ToArray();

            string[] included = blackListStatus
                .Where(file => !file.Status.Item1)
                .Select(file => file.File)
                .ToArray();

            var excluded = blackListStatus
                .Where(file => file.Status.Item1)
                .ToArray();

            if (included.Length > 1)
            {
                logger.Error("Expected exactly 1 solution file, found {Length}, {V}", included.Length, string.Join(", ", included));
                return ExitCode.Failure;
            }

            if (included.Length == 0)
            {
                logger.Error("Expected exactly 1 solution file, found 0");
                return ExitCode.Failure;
            }

            if (excluded.Length > 0)
            {
                logger.Warning("Found blacklisted solution files: {V}", string.Join(", ", excluded.Select(excludedItem => $"{excludedItem.File} ({excludedItem.Status.Item2})")));
            }

            string solutionFile = included.Single();

            ExitCode result = await ProcessHelper.ExecuteAsync(
                msbuildExePath,
                new[] { solutionFile, "/t:restore" },
                logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
