using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class DotNetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.DotNetRestoreEnabled, false);

            if (!enabled)
            {
                return ExitCode.Success;
            }

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                var sb = new List<string>(10);

                string winDir = Environment.GetEnvironmentVariable("WINDIR");

                if (string.IsNullOrWhiteSpace(winDir))
                {
                    logger.WriteError("Error finding Windows directory");
                    return ExitCode.Failure;
                }

                string whereExePath = Path.Combine(winDir, "System32", "where.exe");

                ExitCode exitCode = await Processing.ProcessRunner.ExecuteAsync(
                    whereExePath,
                    arguments: new[] { "dotnet.exe" },
                    standardOutLog: (message, _) => sb.Add(message),
                    cancellationToken: cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    logger.Write("Failed to find dotnet.exe");
                    return exitCode;
                }

                dotNetExePath = sb.FirstOrDefault(item => item.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))?.Trim();
            }
            else if (!File.Exists(dotNetExePath))
            {
                logger.WriteError(
                    $"The specified path to dotnet.exe is from variable '{WellKnownVariables.DotNetExePath}' is set to '{dotNetExePath}' but the file does not exist");
                return ExitCode.Failure;
            }

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Write(
                    $"Path to 'dotnet.exe' has not been specified, set variable '{WellKnownVariables.DotNetExePath}' or ensure the dotnet.exe is installed in its standard location");
                return ExitCode.Failure;
            }

            var pathLookupSpecification = new PathLookupSpecification();
            FileInfo[] solutionFiles = new DirectoryInfo(rootPath)
                .GetFiles("*.sln", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, rootPath))
                .ToArray();

            foreach (FileInfo solutionFile in solutionFiles)
            {
                ExitCode result = await ProcessHelper.ExecuteAsync(
                    dotNetExePath,
                    new[] { "restore", solutionFile.FullName },
                    logger,
                    cancellationToken: cancellationToken);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }
    }
}
