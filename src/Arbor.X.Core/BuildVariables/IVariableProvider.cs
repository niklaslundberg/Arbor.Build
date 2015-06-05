using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.BuildVariables
{
    public interface IVariableProvider
    {
        Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken);

        int Order { get; }
    }
}