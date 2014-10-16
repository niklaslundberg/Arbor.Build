using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using DelegateLogger = Arbor.Sorbus.Core.DelegateLogger;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Versioning
{
    [Priority(1000, runAlways: true)]
    public class AssemblyInfoUnpatcher : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, defaultValue: true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.WriteWarning("Assembly version pathcing is disabled");
                return Task.FromResult(ExitCode.Success);
            }
            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var app =
                new AssemblyPatcherApp(new DelegateLogger(error: logger.WriteError, warning: logger.WriteWarning,
                    info: logger.Write, verbose: logger.WriteVerbose, debug: logger.WriteDebug) { LogLevel = LogLevel.TryParse(logger.LogLevel.Level) });

            try
            {
                logger.WriteVerbose(
                    string.Format("Un-patching assembly info files for directory source root directory '{0}'",
                        sourceRoot));

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