using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Versioning
{
    [Priority(1000, runAlways: true)]
    public class AssemblyInfoUnpatcher : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var assemblyVersionPatchingEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, defaultValue: true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.WriteWarning("Assembly version pathcing is disabled");
                return Task.FromResult(ExitCode.Success);
            }
            var sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var app = new AssemblyPatcherApp();

            try
            {
                app.Unpatch(sourceRoot);
            }
            catch (Exception ex)
            {
                logger.WriteError(string.Format("Could not unpatch. {0}", ex));
                return Task.FromResult(ExitCode.Failure);
            }
            return Task.FromResult(ExitCode.Success);
        }
    }
}