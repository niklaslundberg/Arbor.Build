using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Exceptions;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Cleanup;

[UsedImplicitly]
[Priority(int.MaxValue, true)]
public class ProcessCleanup : ITool
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    private static ImmutableArray<string> ProcessNames { get; } = new string[] {"msbuild.exe", "vbcscompiler.exe", "csc.exe"}.ToImmutableArray();

    public ProcessCleanup(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

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
            IEnumerable<string> shutdownArguments = new List<string>(2)
            {
                "build-server",
                "shutdown"
            };

            var exitCode = await ProcessRunner.ExecuteProcessAsync(
                    _fileSystem.ConvertPathToInternal(dotNetExe.ParseAsPath()),
                    shutdownArguments,
                    Log,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.Debug("Dotnet build server shutdown exit code {ExitCode}", exitCode.Code);
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
            _logger.Warning(ex, "Could not stop all build processes");
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
            _logger.Warning(ex, "Could not stop process {Process}", process.Id);
        }
    }

    private void Log(string message, string category) => _logger.Debug("{Message}", message);
}