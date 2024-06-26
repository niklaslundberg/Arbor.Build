﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Exceptions;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Build.Core.GenericExtensions.Int;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.DotNet;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.FS;
using Arbor.Processing;
using Arbor.Tooler;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Bootstrapper;

public class AppBootstrapper(ILogger logger, IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
{
    private const string BuildToolPackageName = ArborConstants.ArborBuild;
    private const int MaxBuildTimeInSeconds = 900;
    private static readonly string Prefix = $"[{ArborConstants.ArborBuild}.{nameof(AppBootstrapper)}] ";
    private bool _directoryCloneEnabled;

    private bool _failed;
    private BootstrapStartOptions _startOptions = null!;

    public async Task<ExitCode> StartAsync(string[] args)
    {
        logger.Information("Running Arbor.Build Bootstrapper process id {ProcessId}, executable {Executable}",
            Environment.ProcessId,
            typeof(AppBootstrapper).Assembly.Location);

        BootstrapStartOptions startOptions;

        if (Debugger.IsAttached)
        {
            startOptions = await StartWithDebuggerAsync(args);
        }
        else
        {
            startOptions = BootstrapStartOptions.Parse(args);
        }

        ExitCode exitCode = await StartAsync(startOptions);

        logger.Debug("Bootstrapper exit code: {ExitCode}", exitCode);

        if (_failed)
        {
            exitCode = ExitCode.Failure;
        }

        return exitCode;
    }

    public async Task<ExitCode> StartAsync(BootstrapStartOptions? startOptions)
    {
        _startOptions = startOptions ?? new BootstrapStartOptions([]);

        SetEnvironmentVariables();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        ExitCode exitCode;

        try
        {
            exitCode = await TryStartAsync(_startOptions);

            stopwatch.Stop();
        }
        catch (AggregateException ex)
        {
            stopwatch.Stop();
            exitCode = ExitCode.Failure;
            logger.Error(ex, "{Prefix}", Prefix);

            foreach (Exception innerEx in ex.InnerExceptions)
            {
                logger.Error(innerEx, "{Prefix}", Prefix);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            exitCode = ExitCode.Failure;
            logger.Error(ex, "{Prefix} Could not start process", Prefix);
        }

        bool parsed = environmentVariables.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds)
            .TryParseInt32(out int exitDelayInMilliseconds);

        if (parsed && exitDelayInMilliseconds > 0)
        {
            logger.Information(
                "Delaying bootstrapper exit with {ExitDelayInMilliseconds} milliseconds as specified in '{BootstrapperExitDelayInMilliseconds}'",
                exitDelayInMilliseconds,
                WellKnownVariables.BootstrapperExitDelayInMilliseconds);

            await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds));
        }

        logger.Information(
            "Arbor.Build.Bootstrapper total inclusive Arbor.Build elapsed time in seconds: {ElapsedSeconds}",
            stopwatch.Elapsed.TotalSeconds.ToString("F", CultureInfo.InvariantCulture));

        return exitCode;
    }

    private static void KillAllProcessesSpawnedBy(uint parentProcessId, ILogger logger)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return;
        }

#pragma warning disable CA1416 // Validate platform compatibility
        logger.Debug("Finding processes spawned by process with Id [{ParentProcessId}]", parentProcessId);

        ManagementObjectCollection collection;

        using (var searcher =
               new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessId={parentProcessId}"))
        {
            collection = searcher.Get();
        }

        if (collection.Count > 0)
        {
            logger.Debug("Killing [{Count}] processes spawned by process with Id [{ParentProcessId}]",
                collection.Count,
                parentProcessId);

            foreach (ManagementBaseObject item in collection)
            {
                uint childProcessId = (uint)item["ProcessId"];

                if ((int)childProcessId != Environment.ProcessId)
                {
                    KillAllProcessesSpawnedBy(childProcessId, logger);

                    try
                    {
                        using var childProcess = Process.GetProcessById((int)childProcessId);

                        if (!childProcess.HasExited)
                        {
                            logger.Debug("Killing child process [{ProcessName}] with Id [{ChildProcessId}]",
                                childProcess.ProcessName,
                                childProcessId);

                            childProcess.Kill();

                            logger.Verbose("Child process with id {ChildProcessId} was killed", childProcessId);
                        }
                    }
                    catch (Exception ex) when (!ex.IsFatal() &&
                                               ex is ArgumentException or InvalidOperationException)
                    {
                        logger.Warning("Child process with id {ChildProcessId} could not be killed",
                            childProcessId);
                    }
                }
            }
        }

#pragma warning restore CA1416 // Validate platform compatibility
    }

    private async Task<BootstrapStartOptions> StartWithDebuggerAsync(string[] args)
    {
        var startOptions = BootstrapStartOptions.Parse(args);

        var baseDir = new DirectoryEntry(fileSystem, (VcsPathHelper.FindVcsRootPath(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Could not get base directory")).ParseAsPath());

        var tempDirectory = new DirectoryEntry(fileSystem, UPath.Combine(
            Path.GetTempPath().ParseAsPath(),
            $"{DefaultPaths.TempPathPrefix}_Boot_Debug",
            DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)));

        tempDirectory.EnsureExists();

        WriteDebug($"Using temp directory '{tempDirectory}'");

        var paths = new PathLookupSpecification();

        await DirectoryCopy.CopyAsync(baseDir, tempDirectory, pathLookupSpecificationOption: paths);

        environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchNameVersionOverrideEnabled, "true");
        environmentVariables.SetEnvironmentVariable(WellKnownVariables.VariableOverrideEnabled, "true");

        var bootstrapStartOptions = new BootstrapStartOptions(
            args,
            tempDirectory,
            true,
            "refs/heads/develop/12.34.56",
            downloadOnly: startOptions.DownloadOnly,
            arborBuildExePath: startOptions.ArborBuildExePath);

        WriteDebug("Starting with debugger attached");

        return bootstrapStartOptions;
    }

    private void WriteDebug(string message)
    {
        Debug.WriteLine(Prefix + message);
        logger.Debug("{Prefix}{Message}", Prefix, message);
    }

    private void SetEnvironmentVariables()
    {
        if (!string.IsNullOrWhiteSpace(_startOptions.BaseDir?.FullName) && _startOptions.BaseDir.Exists)
        {
            environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, fileSystem.ConvertPathToInternal(_startOptions.BaseDir.Path));
        }

        if (_startOptions.PreReleaseEnabled == true)
        {
            environmentVariables.SetEnvironmentVariable(
                WellKnownVariables.AllowPreRelease,
                _startOptions!.PreReleaseEnabled!.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(_startOptions.BranchName))
        {
            environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, _startOptions.BranchName);
        }
    }

    private async Task<ExitCode> TryStartAsync(BootstrapStartOptions startOptions)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        string? version = fileVersionInfo.FileVersion;

        logger.Information("Starting Arbor.Build Bootstrapper version {Version}", version);

        string? directoryCloneValue = environmentVariables.GetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled);

        _ = directoryCloneValue
            .TryParseBool(out bool directoryCloneEnabled, true);

        _directoryCloneEnabled = directoryCloneEnabled;

        if (!_directoryCloneEnabled)
        {
            logger.Verbose("Environment variable '{DirectoryCloneEnabled}' has value '{DirectoryCloneValue}'",
                WellKnownVariables.DirectoryCloneEnabled,
                directoryCloneValue);
        }

        var baseDir = await GetBaseDirectoryAsync(_startOptions);

        DirectoryEntry buildDir = new DirectoryEntry(fileSystem, UPath.Combine(baseDir.Path, "build")).EnsureExists();

        logger.Verbose("Using base directory '{BaseDir}'", baseDir);

        logger.Debug("Downloading nuget package {Package}", BuildToolPackageName);
        DirectoryEntry buildToolsDirectory;
        try
        {
            buildToolsDirectory =
                await DownloadNuGetPackageAsync(startOptions);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            throw new InvalidOperationException("Could not download build tools", ex);
        }

        if (!buildToolsDirectory.Exists)
        {
            logger.Error("Arbor.Build package download directory {Path} does not exist", fileSystem.ConvertPathToInternal(buildToolsDirectory.Path));
            return ExitCode.Failure;
        }

        ExitCode exitCode;

        try
        {
            if (startOptions.DownloadOnly)
            {
                exitCode = ExitCode.Success;
            }
            else
            {
                ExitCode buildToolsResult =
                    await RunBuildToolsAsync(buildDir.Path, buildToolsDirectory, startOptions.ArborBuildExePath);

                if (buildToolsResult.IsSuccess)
                {
                    logger.Information("The build tools succeeded");
                }
                else
                {
                    logger.Error("The build tools process was not successful, exit code {BuildToolsResult}",
                        buildToolsResult);
                }

                exitCode = buildToolsResult;
            }
        }
        catch (TaskCanceledException)
        {
            try
            {
                bool parsed = environmentVariables.GetEnvironmentVariable("KillSpawnedProcess").TryParseBool(out bool enabled, true);

                if (parsed && enabled)
                {
                    KillAllProcessesSpawnedBy((uint)Environment.ProcessId, logger);
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not kill process");
            }

            logger.Error("The build timed out");
            exitCode = ExitCode.Failure;
            _failed = true;
        }

        return exitCode;
    }

    private async Task<DirectoryEntry> DownloadNuGetPackageAsync(BootstrapStartOptions startOptions)
    {
        string? version = environmentVariables.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageVersion);

        string? nuGetSource = environmentVariables.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageSource);

        bool parsed = environmentVariables.GetEnvironmentVariable(WellKnownVariables.AllowPreRelease)
            .TryParseBool(out bool parsedValue);

        bool preReleaseIsAllowed = _startOptions.PreReleaseEnabled ?? (parsed && parsedValue);

        bool parsedPackageVersion = NuGetPackageVersion.TryParse(version, out NuGetPackageVersion? packageVersion);

        if (!parsedPackageVersion)
        {
            packageVersion = NuGetPackageVersion.LatestAvailable;
        }

        var nuGetPackageInstaller = new NuGetPackageInstaller(logger: logger);

        var nuGetPackage = new NuGetPackage(
            new NuGetPackageId(BuildToolPackageName),
            packageVersion);

        var nugetPackageSettings = new NugetPackageSettings
        {
            AllowPreRelease = preReleaseIsAllowed,
            NugetSource = nuGetSource,
            NugetConfigFile = startOptions.NuGetConfig,
            UseCli = false,
            Extract = true,
        };

        var tempInstallDirectory = startOptions.TempDirectory is { }
            ? new DirectoryInfo(startOptions.TempDirectory.ConvertPathToInternal())
            : null;

        var nuGetPackageInstallResult = await nuGetPackageInstaller.InstallPackageAsync(
                nuGetPackage,
                nugetPackageSettings, installBaseDirectory: tempInstallDirectory)
            ;

        if (nuGetPackageInstallResult.SemanticVersion is null)
        {
            logger.Warning("Could not download {PackageVersion}", packageVersion);
        }

        if (!parsedPackageVersion
            && nuGetPackageInstallResult.SemanticVersion is null
            && nuGetPackage.NuGetPackageVersion != NuGetPackageVersion.LatestDownloaded)
        {
            logger.Information("Retrying package download of {PackageVersion} with latest downloaded",
                packageVersion);

            nuGetPackageInstallResult = await nuGetPackageInstaller.InstallPackageAsync(
                    new NuGetPackage(nuGetPackage.NuGetPackageId, NuGetPackageVersion.LatestDownloaded),
                    nugetPackageSettings)
                ;
        }

        if (nuGetPackageInstallResult.SemanticVersion is null ||
            nuGetPackageInstallResult.PackageDirectory is null)
        {
            throw new InvalidOperationException(
                $"Could not download {packageVersion}, verify it exists and that all sources are available");
        }

        return new DirectoryEntry(fileSystem,
            fileSystem.ConvertPathFromInternal(nuGetPackageInstallResult.PackageDirectory.FullName));
    }

    private Task<DirectoryEntry> GetBaseDirectoryAsync(BootstrapStartOptions startOptions)
    {
        DirectoryEntry baseDir;

        if (!string.IsNullOrWhiteSpace(startOptions.BaseDir?.FullName) && startOptions.BaseDir.Exists)
        {
            logger.Information("Using base directory '{BaseDir}' from start options", startOptions.BaseDir);

            baseDir = startOptions.BaseDir;
        }
        else
        {
            string? foundPath = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

            if (string.IsNullOrWhiteSpace(foundPath))
            {
                throw new InvalidOperationException("Could not get source root path");
            }

            baseDir = new DirectoryEntry(fileSystem, foundPath.ParseAsPath());
        }

        return Task.FromResult(baseDir);
    }

    private async Task<(UPath?, List<string>)> GetExePath(DirectoryEntry buildToolDirectory, CancellationToken cancellationToken)
    {
        var buildExeFile = buildToolDirectory.GetFiles("Arbor.Build.exe");

        if (buildExeFile.Length == 1)
        {
            return (buildExeFile.Single().Path, []);
        }

        var arborBuild =
            buildToolDirectory.GetFiles("Arbor.Build.*")
                .Where(file => !file.Name.Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (arborBuild.Count != 1)
        {
            PrintInvalidExeFileCount(arborBuild, buildToolDirectory.FullName);
            return (null, []);
        }

        FileEntry? buildToolExecutable = arborBuild.SingleOrDefault(file => file.ExtensionWithDot?.Equals(".exe", StringComparison.OrdinalIgnoreCase) ?? false);

        if (buildToolExecutable is {})
        {
            return (buildToolExecutable.Path, []);
        }

        FileEntry? buildToolDll = arborBuild.SingleOrDefault(file => file.ExtensionWithDot?.Equals(".dll", StringComparison.OrdinalIgnoreCase) ?? false);

        if (buildToolDll is null)
        {
            return (null, []);
        }

        var variables = await new DotNetEnvironmentVariableProvider(environmentVariables, fileSystem)
            .GetBuildVariablesAsync(
                logger,
                [],
                cancellationToken);

        string? dotnetExePath = variables.SingleOrDefault(variable =>
            variable.Key.Equals(WellKnownVariables.DotNetExePath, StringComparison.OrdinalIgnoreCase))?.Value;

        if (string.IsNullOrWhiteSpace(dotnetExePath))
        {
            logger.Error("Could not find dotnet.exe");
            return (null, []);
        }

        return (dotnetExePath, ["--", buildToolDll.ConvertPathToInternal()]);
    }

    private async Task<ExitCode> RunBuildToolsAsync(UPath buildDir, DirectoryEntry buildToolDirectory, string? arborBuildExePath)
    {
        const string timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
        string? timeoutInSecondsFromEnvironment = environmentVariables.GetEnvironmentVariable(timeoutKey);

        if (timeoutInSecondsFromEnvironment.TryParseInt32(out int parseResult, MaxBuildTimeInSeconds))
        {
            logger.Verbose("Using timeout from environment variable {TimeoutKey}", timeoutKey);
        }

        int usedTimeoutInSeconds = parseResult;

        logger.Information("Using build timeout {UsedTimeoutInSeconds} seconds", usedTimeoutInSeconds);

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds));
        const string buildApplicationPrefix = "[Arbor.Build] ";

        var arguments = new List<string>();

        UPath? exePath = arborBuildExePath is {} value ? value : (UPath?) null;
        if (string.IsNullOrWhiteSpace(arborBuildExePath))
        {
            var (defaultExePath, args) = await GetExePath(buildToolDirectory, cancellationTokenSource.Token);

            arguments.AddRange(args);
            exePath = defaultExePath;
        }

        if (exePath is null)
        {
            return ExitCode.Failure;
        }

        arguments.Add($"-buildDirectory={fileSystem.ConvertPathToInternal(buildDir)}");

        if (_startOptions.Args.Any())
        {
            arguments.AddRange(_startOptions.Args);
        }

        return await ProcessRunner.ExecuteProcessAsync(fileSystem.ConvertPathToInternal(exePath.Value),
                arguments,
                (message, _) => logger.Information("{Prefix}{Message}", buildApplicationPrefix, message),
                (message, _) => logger.Error("{Prefix}{Message}", buildApplicationPrefix, message),
                logger.Information,
                logger.Verbose,
                cancellationToken: cancellationTokenSource.Token)
            ;
    }

    private void PrintInvalidExeFileCount(List<FileEntry> exeFiles, string buildToolDirectoryPath)
    {
        string multiple =
            $"Found {exeFiles.Count} such files: {string.Join(", ", exeFiles.Select(file => file.Name))}";

        const string single = ". Found no such files";

        string found = exeFiles.Count > 0
            ? single
            : multiple;

        logger.Error(
            "Expected directory {BuildToolDirectoryPath} to contain exactly one executable file with extensions .exe. {Found}",
            buildToolDirectoryPath,
            found);
    }
}