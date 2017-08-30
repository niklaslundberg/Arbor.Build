using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
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

            string dotNetExePath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, "dotnet");

            ExitCode result = await ProcessHelper.ExecuteAsync(dotNetExePath, new[] { "restore", rootPath }, logger);

            return result;
        }
    }
}
