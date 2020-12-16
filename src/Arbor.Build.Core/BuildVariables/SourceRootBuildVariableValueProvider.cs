using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.BuildVariables
{
    [UsedImplicitly]
    public class SourceRootBuildVariableValueProvider : IVariableProvider
    {
        private readonly DirectoryEntry? _sourceDirectory;

        public SourceRootBuildVariableValueProvider(SourceRootValue? sourceDirectory = null) =>
            _sourceDirectory = sourceDirectory?.SourceRoot;

        public int Order => int.MinValue;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (_sourceDirectory is {})
            {
                logger.Verbose("Source directory is specified as {SourceDirectory}", _sourceDirectory);
                variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, _sourceDirectory.FileSystem.ConvertPathToInternal(_sourceDirectory.Path.FullName)));
            }

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}