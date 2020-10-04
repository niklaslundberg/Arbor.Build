using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Processing;
using Arbor.Sorbus.Core;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Versioning
{
    [UsedImplicitly]
    [Priority(1000, true)]
    public class AssemblyInfoUnpatcher : ITool
    {
        private readonly IFileSystem _fileSystem;
        private readonly BuildContext _buildContext;

        public AssemblyInfoUnpatcher(IFileSystem fileSystem, BuildContext buildContext)
        {
            _fileSystem = fileSystem;
            _buildContext = buildContext;
        }

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

            var sourceRoot = _buildContext.SourceRoot;

            var app = new AssemblyPatcherApp();

            try
            {
                logger.Verbose("Un-patching assembly info files for directory source root directory '{SourceRoot}'",
                    sourceRoot);

                app.Unpatch(_fileSystem.ConvertPathToInternal(sourceRoot.Path));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not un-patch.");
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}
