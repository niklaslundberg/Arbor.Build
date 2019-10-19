using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.BuildVariables
{
    [UsedImplicitly]
    public class SourceRootBuildVariableValueProvider : IVariableProvider
    {
        private readonly string? _sourceDirectory;

        public SourceRootBuildVariableValueProvider(SourceRootValue? sourceDirectory = null) =>
            _sourceDirectory = sourceDirectory?.SourceRoot;

        public int Order => int.MinValue;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (!string.IsNullOrWhiteSpace(_sourceDirectory))
            {
                logger.Verbose("Source directory is specified as {SourceDirectory}", _sourceDirectory);
                variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, _sourceDirectory));
            }

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}