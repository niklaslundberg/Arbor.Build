using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using Arbor.Sorbus.Core;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Versioning
{
    [UsedImplicitly]
    [Priority(1000, true)]
    public class AssemblyInfoUnpatcher : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.Warning("Assembly version patching is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var app = new AssemblyPatcherApp();

            try
            {
                logger.Verbose("Un-patching assembly info files for directory source root directory '{SourceRoot}'",
                    sourceRoot);

                app.Unpatch(sourceRoot);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not unpatch.");
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}
