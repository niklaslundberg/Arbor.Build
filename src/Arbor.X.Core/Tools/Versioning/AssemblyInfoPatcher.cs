using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using DelegateLogger = Arbor.Sorbus.Core.DelegateLogger;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Versioning
{
    [Priority(200)]
    public class AssemblyInfoPatcher : ITool
    {
        string _filePattern;

        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var delegateLogger = new DelegateLogger(error: logger.WriteError, warning: logger.WriteWarning,
                info: logger.Write, verbose: logger.WriteVerbose, debug: logger.WriteDebug) { LogLevel = LogLevel.TryParse(logger.LogLevel.Level) };

            var app = new AssemblyPatcherApp(delegateLogger);

            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, defaultValue: true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.WriteWarning("Assembly version patching is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            _filePattern = buildVariables.GetVariableValueOrDefault(WellKnownVariables.AssemblyFilePatchingFilePattern,
                "AssemblyInfo.cs");

            logger.WriteVerbose(string.Format("Using assembly version file pattern '{0}' to lookup files to patch", _filePattern));

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            IVariable netAssemblyVersionVar =
                buildVariables.SingleOrDefault(@var => @var.Key == WellKnownVariables.NetAssemblyVersion);
            string netAssemblyVersion;

            if (netAssemblyVersionVar == null || string.IsNullOrWhiteSpace(netAssemblyVersionVar.Value))
            {
                logger.WriteWarning(string.Format("The build variable {0} is not defined or empty",
                    WellKnownVariables.NetAssemblyVersion));
                netAssemblyVersion = "0.0.1.0";

                logger.WriteWarning(string.Format("Using fall-back version {0}", netAssemblyVersion));
            }
            else
            {
                netAssemblyVersion = netAssemblyVersionVar.Value;
            }

            var assemblyVersion = new Version(netAssemblyVersion);


            IVariable netAssemblyFileVersionVar =
                buildVariables.SingleOrDefault(@var => @var.Key == WellKnownVariables.NetAssemblyFileVersion);
            string netAssemblyFileVersion;

            if (netAssemblyFileVersionVar == null || string.IsNullOrWhiteSpace(netAssemblyFileVersionVar.Value))
            {
                logger.WriteWarning(string.Format("The build variable {0} is not defined or empty",
                    WellKnownVariables.NetAssemblyFileVersion));
                netAssemblyFileVersion = "0.0.1.1";

                logger.WriteWarning(string.Format("Using fall-back version {0}", netAssemblyFileVersion));
            }
            else
            {
                netAssemblyFileVersion = netAssemblyFileVersionVar.Value;
            }

            var assemblyFileVersion = new Version(netAssemblyFileVersion);

            try
            {
                logger.WriteVerbose(
                    string.Format(
                        "Patching assembly info files with assembly version {0}, assembly file version {1} for directory source root directory '{2}'",
                        assemblyVersion, assemblyFileVersion, sourceRoot));
                
                app.Patch(new AssemblyVersion(assemblyVersion), new AssemblyFileVersion(assemblyFileVersion), sourceRoot, assemblyfilePattern: _filePattern);
            }
            catch (Exception ex)
            {
                logger.WriteError(string.Format("Could not patch assembly infos. {0}", ex));
                return Task.FromResult(ExitCode.Failure);
            }
            return Task.FromResult(ExitCode.Success);
        }
    }
}