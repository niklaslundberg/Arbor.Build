using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using JetBrains.Annotations;

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
                .Select(file => new { File = file, Status = pathLookupSpecification.IsFileBlackListed(file) })
                .ToArray();

            string[] included = blackListStatus
                .Where(file => !file.Status.Item1)
                .Select(file => file.File)
                .ToArray();

            var excluded = blackListStatus
                .Where(file => file.Status.Item1)
                .ToArray();

            if (included.Length != 1)
            {
                logger.WriteError($"Expected exactly 1 solution file, found {solutionFiles.Length}");
                return ExitCode.Failure;
            }

            if (excluded.Length > 0)
            {
                logger.WriteWarning($"Found blacklisted solution files: {string.Join(", ", excluded.Select(excludedItem => $"{excludedItem.File} ({excludedItem.Status.Item2})"))}");
            }

            string solutionFile = solutionFiles.Single();

            ExitCode result = await ProcessHelper.ExecuteAsync(
                msbuildExePath,
                new[] { solutionFile, "/t:restore" },
                logger,
                cancellationToken: cancellationToken);

            return result;
        }
    }
}
