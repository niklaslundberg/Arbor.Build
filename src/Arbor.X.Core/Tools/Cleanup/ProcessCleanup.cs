using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Exceptions;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Cleanup
{
    [Priority(1001, runAlways: true)]
    public class ProcessCleanup : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.CleanupProcessesAfterBuildEnabled,
                defaultValue: false);

            if (!enabled)
            {
                logger.Write($"Process cleanup is disabled, enable by setting key {WellKnownVariables.CleanupProcessesAfterBuildEnabled} to true");
                return ExitCode.Success.AsCompletedTask();
            }

            logger.Write($"Process cleanup is enabled, from key {WellKnownVariables.CleanupProcessesAfterBuildEnabled} to true");

            string sourceRoot = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot, defaultValue: string.Empty);

            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                return ExitCode.Success.AsCompletedTask();
            }

            if (!Directory.Exists(sourceRoot))
            {
                return ExitCode.Success.AsCompletedTask();
            }

            var procesNamesToKill = new[] { "VBCSCompiler.exe", "csc.exe" };

            bool ShouldKillProcess(Process process)
            {
                if (process == null)
                {
                    return false;
                }

                string executablePath = process.ExecutablePath();

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return false;
                }

                if (process.HasExited)
                {
                    return false;
                }

                string fileName = Path.GetFileName(executablePath);

                if (!procesNamesToKill.Any(processToKill => processToKill.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                if (executablePath.IndexOf(sourceRoot, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                logger.WriteVerbose($"Found process {process.ToDisplayValue()} to kill in cleanup");

                return true;
            }

            void TryKillProcess(Process process)
            {
                if (process == null)
                {
                    return;
                }

                try
                {
                    if (process.HasExited)
                    {
                        return;
                    }

                    logger.WriteVerbose($"Killing process {process.ToDisplayValue()}");

                    process.Kill();

                    logger.WriteVerbose($"Killed process {process.ToDisplayValue()}");
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.WriteVerbose($"Could not kill process {process.ToDisplayValue()}");
                }
            }

            ImmutableArray<Process> processesToKill = Process.GetProcesses()
                .Where(ShouldKillProcess)
                .ToImmutableArray();

            string message = $"Found [{processesToKill.Length}] processes to kill in cleanup: {Environment.NewLine}{string.Join(Environment.NewLine, processesToKill.Select(process => process.ExecutablePath()))}";

            logger.WriteVerbose(message);

            foreach (Process process in processesToKill)
            {
                TryKillProcess(process);
            }

            return ExitCode.Success.AsCompletedTask();
        }
    }
}
