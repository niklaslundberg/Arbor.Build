using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Castanea;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(100)]
    public class NuGetRestorer : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var app = new CastaneaApplication();

            var vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var files = Directory.GetFiles(vcsRoot, "repositories.config", SearchOption.AllDirectories);

            foreach (var repositoriesConfig in files)
            {
                try
                {
                    int result = app.RestoreAllSolutionPackages(new NuGetConfig
                                                                    {
                                                                        RepositoriesConfig = repositoriesConfig
                                                                    });


                    if (result != 0)
                    {
                        logger.Write(string.Format("Failed to restore {0} package configurations defined in {1}", result, repositoriesConfig));
                        return Task.FromResult(new ExitCode(result));
                    }

                    logger.Write(string.Format("Restored {0} package configurations defined in {1}", result, repositoriesConfig));
                }
                catch (Exception ex)
                {
                    logger.WriteError(string.Format("Cloud not restore packages defined in '{0}'. {1}", repositoriesConfig, ex));
                    return Task.FromResult(ExitCode.Failure);
                }
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}