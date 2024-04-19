using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.GenericExtensions;
using Arbor.FS;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.BuildVariables;

[UsedImplicitly]
public class SourceRootBuildVariableValueProvider(SourceRootValue? sourceDirectory = null) : IVariableProvider
{
    private readonly DirectoryEntry? _sourceDirectory = sourceDirectory?.SourceRoot;

    public int Order => int.MinValue;

    public Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        var variables = new List<IVariable>();

        if (_sourceDirectory is {})
        {
            logger.Verbose("Source directory is specified as {SourceDirectory}", _sourceDirectory.ConvertPathToInternal());
            variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, _sourceDirectory.ConvertPathToInternal()));
        }

        return Task.FromResult(variables.ToReadOnlyCollection());
    }
}