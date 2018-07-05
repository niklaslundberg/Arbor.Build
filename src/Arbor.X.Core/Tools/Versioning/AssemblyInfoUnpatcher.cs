using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using JetBrains.Annotations;
using DelegateLogger = Arbor.Sorbus.Core.DelegateLogger;
using ILogger = Serilog.ILogger;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    [Priority(1000, runAlways: true)]
    public class AssemblyInfoUnpatcher : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.Warning("Assembly version pathcing is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var delegateLogger = new DelegateLogger(
                logger.Error,
                logger.Warning,
                logger.Information,
                logger.Verbose,
                logger.Debug)
            {
                LogLevel = LogLevel.Debug
            };

            var app = new AssemblyPatcherApp(delegateLogger);

            try
            {
                logger.Verbose("Un-patching assembly info files for directory source root directory '{SourceRoot}'", sourceRoot);

                app.Unpatch(sourceRoot);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not unpatch. {Ex}");
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}
