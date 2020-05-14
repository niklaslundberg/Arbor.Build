using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using Serilog;

namespace Arbor.Build.Core.Tools.Scripts
{
    [Priority(840)]
    public class ScriptTool : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            var paths = buildVariables.GetValues(WellKnownVariables.PostScripts);

            var root = buildVariables.GetVariable(WellKnownVariables.SourceRoot).GetValueOrThrow();

            var fullPaths = paths.Select(path => Path.Combine(root, path)).ToArray();

            logger.Debug("Found PostScripts [{PostScriptCount}] {Scripts}", paths.Length, paths);

            foreach (string fullPath in fullPaths)
            {
                if (!File.Exists(fullPath))
                {
                    logger.Warning("The specified executable post script '{Path}' does not exist, skipping", fullPath);
                    continue;
                }

                var exitCode = await ProcessRunner.ExecuteProcessAsync(fullPath,
                    standardOutLog: (message, category) => logger.Information("{Message}", message),
                    workingDirectory: new DirectoryInfo(root),
                    cancellationToken: cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    logger.Error("Could not execute post script {Path}", fullPath);
                    return ExitCode.Failure;
                }
            }

            return ExitCode.Success;
        }
    }
}