using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Exceptions;
using Arbor.Processing;
using Arbor.Processing;
using Serilog;

namespace Arbor.Build.Core.Tools.Cleanup
{
    [Priority(1001, true)]
    public class ProcessCleanup : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.CleanupProcessesAfterBuildEnabled,
                false);

            if (!enabled)
            {
                logger.Information(
                    "Process cleanup is disabled, enable by setting key {CleanupProcessesAfterBuildEnabled} to true",
                    WellKnownVariables.CleanupProcessesAfterBuildEnabled);
                return ExitCode.Success.AsCompletedTask();
            }

            logger.Information("Process cleanup is enabled, from key {CleanupProcessesAfterBuildEnabled} to true",
                WellKnownVariables.CleanupProcessesAfterBuildEnabled);

            string sourceRoot = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot, string.Empty);

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

                if (!procesNamesToKill.Any(processToKill =>
                    processToKill.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                if (executablePath.IndexOf(sourceRoot, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                logger.Verbose("Found process {V} to kill in cleanup", process.ToDisplayValue());

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

                    logger.Verbose("Killing process {V}", process.ToDisplayValue());

                    process.Kill();

                    logger.Verbose("Killed process {V}", process.ToDisplayValue());
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.Verbose("Could not kill process {V}", process.ToDisplayValue());
                }
            }

            ImmutableArray<Process> processesToKill = Process.GetProcesses()
                .Where(ShouldKillProcess)
                .ToImmutableArray();

            string message =
                $"Found [{processesToKill.Length}] processes to kill in cleanup: {Environment.NewLine}{string.Join(Environment.NewLine, processesToKill.Select(process => process.ExecutablePath()))}";

            logger.Verbose(message);

            foreach (Process process in processesToKill)
            {
                TryKillProcess(process);
            }

            return ExitCode.Success.AsCompletedTask();
        }
    }
}
