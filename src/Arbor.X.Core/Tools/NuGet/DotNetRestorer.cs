using Serilog;
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

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class DotNetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.DotNetRestoreEnabled, false);

            if (!enabled)
            {
                return ExitCode.Success;
            }

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Information("Path to 'dotnet.exe' has not been specified, set variable '{DotNetExePath}' or ensure the dotnet.exe is installed in its standard location", WellKnownVariables.DotNetExePath);
                return ExitCode.Failure;
            }

            var pathLookupSpecification = new PathLookupSpecification();
            FileInfo[] solutionFiles = new DirectoryInfo(rootPath)
                .GetFiles("*.sln", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, rootPath).Item1)
                .ToArray();

            foreach (FileInfo solutionFile in solutionFiles)
            {
                ExitCode result = await ProcessHelper.ExecuteAsync(
                    dotNetExePath,
                    new[] { "restore", solutionFile.FullName },
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
