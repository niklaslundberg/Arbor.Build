using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Help
{
    [Priority(int.MinValue)]
    [UsedImplicitly]
    public class HelpTool : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            if (args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase)))
            {
                logger.Debug("Help invoked skipping other tools");
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}