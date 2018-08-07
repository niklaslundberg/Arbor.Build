using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Arbor.X.Core.BuildVariables
{
    public interface IVariableProvider
    {
        int Order { get; }

        Task<IEnumerable<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken);
    }
}
