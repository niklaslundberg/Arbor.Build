using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.DotNet
{
    [UsedImplicitly]
    public class DotNetEnvironmentVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (!string.IsNullOrWhiteSpace(dotNetExePath))
            {
                return ImmutableArray<IVariable>.Empty;
            }

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                var sb = new List<string>(10);

                string winDir = Environment.GetEnvironmentVariable("WINDIR");

                if (string.IsNullOrWhiteSpace(winDir))
                {
                    logger.Warning("Error finding Windows directory");
                    return ImmutableArray<IVariable>.Empty;
                }

                string whereExePath = Path.Combine(winDir, "System32", "where.exe");

                ExitCode exitCode = await Processing.ProcessRunner.ExecuteAsync(
                    whereExePath,
                    arguments: new[] { "dotnet.exe" },
                    standardOutLog: (message, _) => sb.Add(message),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    logger.Error("Failed to find dotnet.exe with where.exe");
                }

                dotNetExePath =
                    sb.FirstOrDefault(item => item.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))?.Trim();
            }
            else if (!File.Exists(dotNetExePath))
            {
                logger.Warning(
                    "The specified path to dotnet.exe is from variable '{DotNetExePath}' is set to '{DotNetExePath1}' but the file does not exist",
                    WellKnownVariables.DotNetExePath,
                    dotNetExePath);
                return ImmutableArray<IVariable>.Empty;
            }

            return new IVariable[] { new BuildVariable(WellKnownVariables.DotNetExePath, dotNetExePath) }.ToImmutableArray();
        }
    }
}
