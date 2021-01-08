using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Build.Core.GenericExtensions.Int;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.DotNet;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Exceptions;
using Arbor.Processing;
using Arbor.Tooler;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Bootstrapper
{
    public class AppBootstrapper
    {
        private const string BuildToolPackageName = ArborConstants.ArborBuild;
        private const int MaxBuildTimeInSeconds = 900;
        private static readonly string Prefix = $"[{ArborConstants.ArborBuild}.{nameof(AppBootstrapper)}] ";
        private readonly ILogger _logger;
        private bool _directoryCloneEnabled;

        private bool _failed;
        private BootstrapStartOptions _startOptions = null!;
        private readonly IEnvironmentVariables _environmentVariables;
        private readonly IFileSystem _fileSystem;

        public AppBootstrapper(ILogger logger, IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
        {
            _logger = logger;
            _environmentVariables = environmentVariables;
            _fileSystem = fileSystem;
        }

        public async Task<ExitCode> StartAsync(string[] args)
        {
            _logger.Information("Running Arbor.Build Bootstrapper process id {ProcessId}, executable {Executable}",
                Process.GetCurrentProcess().Id,
                typeof(AppBootstrapper).Assembly.Location);

            BootstrapStartOptions startOptions;

            if (Debugger.IsAttached)
            {
                startOptions = await StartWithDebuggerAsync(args).ConfigureAwait(false);
            }
            else
            {
                startOptions = BootstrapStartOptions.Parse(args);
            }

            ExitCode exitCode = await StartAsync(startOptions).ConfigureAwait(false);

            _logger.Debug("Bootstrapper exit code: {ExitCode}", exitCode);

            if (_failed)
            {
                exitCode = ExitCode.Failure;
            }

            return exitCode;
        }

        public async Task<ExitCode> StartAsync(BootstrapStartOptions? startOptions)
        {
            _startOptions = startOptions ?? new BootstrapStartOptions(Array.Empty<string>());

            SetEnvironmentVariables();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ExitCode exitCode;

            try
            {
                exitCode = await TryStartAsync(_startOptions).ConfigureAwait(false);

                stopwatch.Stop();
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _logger.Error(ex, "{Prefix}", Prefix);

                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    _logger.Error(innerEx, "{Prefix}", Prefix);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _logger.Error(ex, "{Prefix} Could not start process", Prefix);
            }

            bool parsed = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds)
                .TryParseInt32(out int exitDelayInMilliseconds);

            if (parsed && exitDelayInMilliseconds > 0)
            {
                _logger.Information(
                    "Delaying bootstrapper exit with {ExitDelayInMilliseconds} milliseconds as specified in '{BootstrapperExitDelayInMilliseconds}'",
                    exitDelayInMilliseconds,
                    WellKnownVariables.BootstrapperExitDelayInMilliseconds);

                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds)).ConfigureAwait(false);
            }

            _logger.Information(
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

                    if ((int)childProcessId != Process.GetCurrentProcess().Id)
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
                                                   (ex is ArgumentException || ex is InvalidOperationException))
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

            var baseDir = new DirectoryEntry(_fileSystem, VcsPathHelper.FindVcsRootPath(AppContext.BaseDirectory).ParseAsPath());

            var tempDirectory = new DirectoryEntry(_fileSystem, UPath.Combine(
                Path.GetTempPath().ParseAsPath(),
                $"{DefaultPaths.TempPathPrefix}_Boot_Debug",
                DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)));

            tempDirectory.EnsureExists();

            WriteDebug($"Using temp directory '{tempDirectory}'");

            var paths = new PathLookupSpecification();

            await DirectoryCopy.CopyAsync(baseDir, tempDirectory, pathLookupSpecificationOption: paths).ConfigureAwait(false);

            _environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchNameVersionOverrideEnabled, "true");
            _environmentVariables.SetEnvironmentVariable(WellKnownVariables.VariableOverrideEnabled, "true");

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
            _logger.Debug("{Prefix}{Message}", Prefix, message);
        }

        private void SetEnvironmentVariables()
        {
            if (!string.IsNullOrWhiteSpace(_startOptions.BaseDir?.FullName) && _startOptions.BaseDir.Exists)
            {
                _environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, _fileSystem.ConvertPathToInternal(_startOptions.BaseDir.Path));
            }

            if (_startOptions.PreReleaseEnabled == true)
            {
                _environmentVariables.SetEnvironmentVariable(
                    WellKnownVariables.AllowPreRelease,
                    _startOptions!.PreReleaseEnabled!.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(_startOptions.BranchName))
            {
                _environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, _startOptions.BranchName);
            }
        }

        private async Task<ExitCode> TryStartAsync(BootstrapStartOptions startOptions)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string? version = fileVersionInfo.FileVersion;

            _logger.Information("Starting Arbor.Build Bootstrapper version {Version}", version);

            string? directoryCloneValue = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled);

            _ = directoryCloneValue
                .TryParseBool(out bool directoryCloneEnabled, true);

            _directoryCloneEnabled = directoryCloneEnabled;

            if (!_directoryCloneEnabled)
            {
                _logger.Verbose("Environment variable '{DirectoryCloneEnabled}' has value '{DirectoryCloneValue}'",
                    WellKnownVariables.DirectoryCloneEnabled,
                    directoryCloneValue);
            }

            var baseDir = await GetBaseDirectoryAsync(_startOptions).ConfigureAwait(false);

            DirectoryEntry buildDir = new DirectoryEntry(_fileSystem, UPath.Combine(baseDir.Path, "build")).EnsureExists();

            _logger.Verbose("Using base directory '{BaseDir}'", baseDir);

            _logger.Debug("Downloading nuget package {Package}", BuildToolPackageName);

            UPath? buildToolsDirectory =
                (await DownloadNuGetPackageAsync().ConfigureAwait(false))?.ParseAsPath();

            if (!buildToolsDirectory.HasValue)
            {
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
                        await RunBuildToolsAsync(buildDir.Path, buildToolsDirectory.Value, startOptions.ArborBuildExePath).ConfigureAwait(false);

                    if (buildToolsResult.IsSuccess)
                    {
                        _logger.Information("The build tools succeeded");
                    }
                    else
                    {
                        _logger.Error("The build tools process was not successful, exit code {BuildToolsResult}",
                            buildToolsResult);
                    }

                    exitCode = buildToolsResult;
                }
            }
            catch (TaskCanceledException)
            {
                try
                {
                    bool parsed = _environmentVariables.GetEnvironmentVariable("KillSpawnedProcess").TryParseBool(out bool enabled, true);

                    if (parsed && enabled)
                    {
                        KillAllProcessesSpawnedBy((uint)Process.GetCurrentProcess().Id, _logger);
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Error(ex, "Could not kill process");
                }

                _logger.Error("The build timed out");
                exitCode = ExitCode.Failure;
                _failed = true;
            }

            return exitCode;
        }

        private async Task<string> DownloadNuGetPackageAsync()
        {
            string? version = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageVersion);

            string? nuGetSource = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageSource);

            bool parsed = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.AllowPreRelease)
                .TryParseBool(out bool parsedValue);

            bool preReleaseIsAllowed = _startOptions.PreReleaseEnabled ?? (parsed && parsedValue);

            bool parsedPackageVersion = NuGetPackageVersion.TryParse(version, out NuGetPackageVersion? packageVersion);

            if (!parsedPackageVersion)
            {
                packageVersion = NuGetPackageVersion.LatestAvailable;
            }

            var nuGetPackageInstaller = new NuGetPackageInstaller(logger: _logger);

            var nuGetPackage = new NuGetPackage(
                new NuGetPackageId(BuildToolPackageName),
                packageVersion);

            var nugetPackageSettings = new NugetPackageSettings(preReleaseIsAllowed, nuGetSource);

            var nuGetPackageInstallResult = await nuGetPackageInstaller.InstallPackageAsync(
                    nuGetPackage,
                    nugetPackageSettings)
                .ConfigureAwait(false);

            if (nuGetPackageInstallResult?.SemanticVersion is null)
            {
                _logger.Warning("Could not download {PackageVersion}", packageVersion);
            }

            if (!parsedPackageVersion
                && nuGetPackageInstallResult?.SemanticVersion is null
                && nuGetPackage.NuGetPackageVersion != NuGetPackageVersion.LatestDownloaded)
            {
                _logger.Information("Retrying package download of {PackageVersion} with latest downloaded",
                    packageVersion);

                nuGetPackageInstallResult = await nuGetPackageInstaller.InstallPackageAsync(
                        new NuGetPackage(nuGetPackage.NuGetPackageId, NuGetPackageVersion.LatestDownloaded),
                        nugetPackageSettings)
                    .ConfigureAwait(false);
            }

            if (nuGetPackageInstallResult?.SemanticVersion is null ||
                nuGetPackageInstallResult.PackageDirectory is null)
            {
                throw new InvalidOperationException(
                    $"Could not download {packageVersion}, verify it exists and that all sources are available");
            }

            return nuGetPackageInstallResult.PackageDirectory.FullName;
        }

        private Task<DirectoryEntry> GetBaseDirectoryAsync(BootstrapStartOptions startOptions)
        {
            DirectoryEntry baseDir;

            if (!string.IsNullOrWhiteSpace(startOptions.BaseDir?.FullName) && startOptions.BaseDir.Exists)
            {
                _logger.Information("Using base directory '{BaseDir}' from start options", startOptions.BaseDir);

                baseDir = startOptions.BaseDir;
            }
            else
            {
                string foundPath = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

                if (string.IsNullOrWhiteSpace(foundPath))
                {
                    throw new InvalidOperationException("Could not get source root path");
                }

                baseDir = new DirectoryEntry(_fileSystem, foundPath.ParseAsPath());
            }

            return Task.FromResult(baseDir);
        }

        private async Task<(UPath?, List<string>)> GetExePath(DirectoryEntry buildToolDirectory, CancellationToken cancellationToken)
        {
            var buildExeFile = buildToolDirectory.GetFiles("Arbor.Build.exe");

            if (buildExeFile.Length == 1)
            {
                return (buildExeFile.Single().Path, new List<string>());
            }

            var arborBuild =
                buildToolDirectory.GetFiles("Arbor.Build.*")
                    .Where(file => !file.Name.Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (arborBuild.Count != 1)
            {
                PrintInvalidExeFileCount(arborBuild, buildToolDirectory.FullName);
                return (null, new List<string>());
            }

            FileEntry? buildToolExecutable = arborBuild.SingleOrDefault(file => file.ExtensionWithDot.Equals(".exe", StringComparison.OrdinalIgnoreCase));

            if (buildToolExecutable is {})
            {
                return (buildToolExecutable.Path, new List<string>());
            }

            FileEntry? buildToolDll = arborBuild.SingleOrDefault(file => file.ExtensionWithDot.Equals(".dll", StringComparison.OrdinalIgnoreCase));

            if (buildToolDll is null)
            {
                return (null, new List<string>());
            }

            ImmutableArray<IVariable> variables = await new DotNetEnvironmentVariableProvider(_environmentVariables, _fileSystem)
                   .GetBuildVariablesAsync(
                       _logger,
                       ImmutableArray<IVariable>.Empty,
                       cancellationToken).ConfigureAwait(false);

            string? dotnetExePath = variables.SingleOrDefault(variable =>
                variable.Key.Equals(WellKnownVariables.DotNetExePath, StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.IsNullOrWhiteSpace(dotnetExePath))
            {
                _logger.Error("Could not find dotnet.exe");
                return (null, new List<string>());
            }

            return (dotnetExePath, new List<string> {"--", buildToolDll.ConvertPathToInternal()});
        }

        private async Task<ExitCode> RunBuildToolsAsync(UPath buildDir, UPath buildToolDirectoryName, string? arborBuildExePath)
        {
            var buildToolDirectory = new DirectoryEntry(_fileSystem, buildToolDirectoryName);

            const string timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            string? timeoutInSecondsFromEnvironment = _environmentVariables.GetEnvironmentVariable(timeoutKey);

            if (timeoutInSecondsFromEnvironment.TryParseInt32(out int parseResult, MaxBuildTimeInSeconds))
            {
                _logger.Verbose("Using timeout from environment variable {TimeoutKey}", timeoutKey);
            }

            int usedTimeoutInSeconds = parseResult;

            _logger.Information("Using build timeout {UsedTimeoutInSeconds} seconds", usedTimeoutInSeconds);

            ExitCode result;

            using (var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds)))
            {
                const string buildApplicationPrefix = "[Arbor.Build] ";

                var arguments = new List<string>();

                UPath? exePath = arborBuildExePath;
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

                arguments.Add($"-buildDirectory={_fileSystem.ConvertPathToInternal(buildDir)}");

                if (_startOptions.Args.Any())
                {
                    arguments.AddRange(_startOptions.Args);
                }

                result = await ProcessRunner.ExecuteProcessAsync(_fileSystem.ConvertPathToInternal(exePath.Value),
                        arguments,
                        (message, _) => _logger.Information("{Prefix}{Message}", buildApplicationPrefix, message),
                        (message, _) => _logger.Error("{Prefix}{Message}", buildApplicationPrefix, message),
                        _logger.Information,
                        _logger.Verbose,
                        cancellationToken: cancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }

            return result;
        }

        private void PrintInvalidExeFileCount(List<FileEntry> exeFiles, string buildToolDirectoryPath)
        {
            string multiple =
                $"Found {exeFiles.Count} such files: {string.Join(", ", exeFiles.Select(file => file.Name))}";

            const string single = ". Found no such files";

            string found = exeFiles.Count > 0
                ? single
                : multiple;

            _logger.Error(
                "Expected directory {BuildToolDirectoryPath} to contain exactly one executable file with extensions .exe. {Found}",
                buildToolDirectoryPath,
                found);
        }
    }
}