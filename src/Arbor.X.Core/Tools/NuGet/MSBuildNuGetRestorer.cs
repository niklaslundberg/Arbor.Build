using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
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
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSBuildNuGetRestoreEnabled,
                defaultValue: false);

            if (!enabled)
            {
                return ExitCode.Success;
            }

            string msbuildExePath =
                buildVariables.GetVariable(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string[] solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            if (solutionFiles.Length != 1)
            {
                logger.WriteError($"Expected exactly 1 solution file, found {solutionFiles.Length}");
                return ExitCode.Failure;
            }

            string solutionFile = solutionFiles.Single();

            ExitCode result = await ProcessHelper.ExecuteAsync(msbuildExePath, new[] {solutionFile, "/t:restore"},
                logger);

            return result;
        }
    }
}