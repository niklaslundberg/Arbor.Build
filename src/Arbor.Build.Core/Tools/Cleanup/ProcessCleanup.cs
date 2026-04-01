using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Exceptions;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Cleanup;

[UsedImplicitly]
[Priority(int.MaxValue, true)]
public class ProcessCleanup(ILogger processLogger, IFileSystem fileSystem) : ITool
{
    private static ImmutableArray<string> ProcessNames { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? [..new[] {"msbuild.exe", "vbcscompiler.exe", "csc.exe"}]
        : [..new[] {"msbuild", "vbcscompiler", "csc"}];

    public async Task<ExitCode> ExecuteAsync(ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        string? dotNetExe = buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath);

        if (!buildVariables.GetBooleanByKey(WellKnownVariables.CleanupProcessesAfterBuildEnabled, true))
        {
            return ExitCode.Success;
        }

        if (!string.IsNullOrWhiteSpace(dotNetExe))
        {
            IEnumerable<string> shutdownArguments =
            [
                "build-server",
                "shutdown"
            ];

            var exitCode = await ProcessRunner.ExecuteProcessAsync(
                    fileSystem.ConvertPathToInternal(dotNetExe.ParseAsPath()),
                    shutdownArguments,
                    Log,
                    cancellationToken: cancellationToken)
                ;

            logger.Debug("Dotnet build server shutdown exit code {ExitCode}", exitCode.Code);
        }

        StopProcesses();

        return ExitCode.Success;
    }

    private void StopProcesses()
    {
        try
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                TryStopProcess(process);
            }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            processLogger.Warning(ex, "Could not stop all build processes");
        }
    }

    private void TryStopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                string? fileName = process.MainModule?.FileName;

                if (!string.IsNullOrWhiteSpace(fileName) && ProcessNames.Any(name =>
                        fileName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    process.Kill(true);
                }
            }
        }
        catch (Win32Exception)
        {
            //ignore
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            processLogger.Warning(ex, "Could not stop process {Process}", process.Id);
        }
    }

    private void Log(string message, string category) => processLogger.Debug("{Message}", message);
}