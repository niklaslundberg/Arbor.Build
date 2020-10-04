using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Processing;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Scripts
{
    [Priority(840)]
    public class ScriptTool : ITool
    {
        private readonly BuildContext _buildContext;

        public ScriptTool(BuildContext buildContext) => _buildContext = buildContext;

        public async Task<ExitCode> ExecuteAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            var paths = buildVariables.GetValues(WellKnownVariables.PostScripts);

            var fullPaths = paths.Select(path => UPath.Combine(_buildContext.SourceRoot.Path, path)).ToArray();

            logger.Debug("Found PostScripts [{PostScriptCount}] {Scripts}", paths.Length, paths);

            foreach (var fullPath in fullPaths)
            {
                if (!_buildContext.SourceRoot.FileSystem.FileExists(fullPath))
                {
                    logger.Warning("The specified executable post script '{Path}' does not exist, skipping", fullPath);
                    continue;
                }

                var exitCode = await ProcessRunner.ExecuteProcessAsync(_buildContext.SourceRoot.FileSystem.ConvertPathToInternal(fullPath),
                    standardOutLog: (message, category) => logger.Information("{Message}", message),
                    workingDirectory: new DirectoryInfo(_buildContext.SourceRoot.FileSystem.ConvertPathToInternal(_buildContext.SourceRoot.Path)),
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