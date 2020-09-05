using System;
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
    public class NuGetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            logger ??= Logger.None ?? throw new ArgumentNullException(nameof(logger));

            bool enabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.NuGetRestoreEnabled);

            if (!enabled)
            {
                logger.Debug("{Tool} is disabled", nameof(NuGetRestorer));
                return ExitCode.Success;
            }

            string nugetExePath =
                buildVariables.GetVariable(WellKnownVariables.ExternalTools_NuGet_ExePath).GetValueOrThrow();

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).GetValueOrThrow();

            string[] solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            PathLookupSpecification pathLookupSpecification =
                DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

            var excludeListStatus = solutionFiles
                .Select(file => new { File = file, Status = pathLookupSpecification.IsFileExcluded(file, rootPath) })
                .ToArray();

            string[] included = excludeListStatus
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
                    string.Join(", ", included));
                return ExitCode.Failure;
            }

            if (included.Length == 0)
            {
                logger.Error("Expected exactly 1 solution file, found 0");
                return ExitCode.Failure;
            }

            if (excluded.Length > 0)
            {
                logger.Warning("Found notallowed solution files: {V}",
                    string.Join(", ",
                        excluded.Select(excludedItem => $"{excludedItem.File} ({excludedItem.Status.Item2})")));
            }

            string solutionFile = included.Single();

            var arguments = new List<string> { "restore", solutionFile };

            ExitCode result = await ProcessHelper.ExecuteAsync(
                nugetExePath,
                arguments,
                logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
