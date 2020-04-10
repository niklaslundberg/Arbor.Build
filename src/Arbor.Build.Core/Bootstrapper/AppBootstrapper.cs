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
using Arbor.Exceptions;
using Arbor.Processing;
using Arbor.Tooler;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Bootstrapper
{
    public class AppBootstrapper
    {
        private const string BuildToolPackageName = ArborConstants.ArborBuild;
        private const int MaxBuildTimeInSeconds = 600;
        private static readonly string Prefix = $"[{ArborConstants.ArborBuild}.{nameof(AppBootstrapper)}] ";
        private readonly ILogger _logger;
        private bool _directoryCloneEnabled;

        private bool _failed;
        private BootstrapStartOptions? _startOptions;

        public AppBootstrapper(ILogger logger) => _logger = logger;

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

        public async Task<ExitCode> StartAsync(BootstrapStartOptions startOptions)
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

            Environment.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds)
                .TryParseInt32(out int exitDelayInMilliseconds);

            if (exitDelayInMilliseconds > 0)
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

                            if (childProcess?.HasExited == false)
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
        }

        private async Task<BootstrapStartOptions> StartWithDebuggerAsync([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            string baseDir = VcsPathHelper.FindVcsRootPath(AppDomain.CurrentDomain.BaseDirectory);

            var tempDirectory = new DirectoryInfo(Path.Combine(
                Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_Boot_Debug",
                DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)));

            tempDirectory.EnsureExists();

            WriteDebug($"Using temp directory '{tempDirectory}'");

            await DirectoryCopy.CopyAsync(baseDir, tempDirectory.FullName).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(WellKnownVariables.BranchNameVersionOverrideEnabled, "true");
            Environment.SetEnvironmentVariable(WellKnownVariables.VariableOverrideEnabled, "true");

            var bootstrapStartOptions = new BootstrapStartOptions(
                Array.Empty<string>(),
                tempDirectory.FullName,
                true,
                "refs/heads/develop/12.34.56");

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
            if (!string.IsNullOrWhiteSpace(_startOptions?.BaseDir) && Directory.Exists(_startOptions.BaseDir))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.SourceRoot, _startOptions.BaseDir);
            }

            if (_startOptions?.PreReleaseEnabled == true)
            {
                Environment.SetEnvironmentVariable(
                    WellKnownVariables.AllowPrerelease,
                    _startOptions!.PreReleaseEnabled!.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(_startOptions?.BranchName))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.BranchName, _startOptions.BranchName);
            }
        }

        private async Task<ExitCode> TryStartAsync(BootstrapStartOptions startOptions)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.FileVersion;

            _logger.Information("Starting Arbor.Build Bootstrapper version {Version}", version);

            string directoryCloneValue = Environment.GetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled);

            _ = directoryCloneValue
                .TryParseBool(out bool directoryCloneEnabled, true);

            _directoryCloneEnabled = directoryCloneEnabled;

            if (!_directoryCloneEnabled)
            {
                _logger.Verbose("Environment variable '{DirectoryCloneEnabled}' has value '{DirectoryCloneValue}'",
                    WellKnownVariables.DirectoryCloneEnabled,
                    directoryCloneValue);
            }

            string baseDir = await GetBaseDirectoryAsync(_startOptions).ConfigureAwait(false);

            DirectoryInfo buildDir = new DirectoryInfo(Path.Combine(baseDir, "build")).EnsureExists();

            _logger.Verbose("Using base directory '{BaseDir}'", baseDir);

            _logger.Debug("Downloading nuget package {Package}", BuildToolPackageName);

            string buildToolsDirectory =
                await DownloadNuGetPackageAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(buildToolsDirectory))
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
                        await RunBuildToolsAsync(buildDir.FullName, buildToolsDirectory).ConfigureAwait(false);

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
                    Environment.GetEnvironmentVariable("KillSpawnedProcess").TryParseBool(out bool enabled, true);

                    if (enabled)
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
            string? version = Environment.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageVersion);

            string? nuGetSource = Environment.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageSource);

            Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                .TryParseBool(out bool preReleaseIsAllowed);

            preReleaseIsAllowed = _startOptions.PreReleaseEnabled ?? preReleaseIsAllowed;

            bool parsedPackageVersion = NuGetPackageVersion.TryParse(version, out NuGetPackageVersion packageVersion);

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
                nuGetPackageInstallResult?.PackageDirectory is null)
            {
                throw new InvalidOperationException(
                    $"Could not download {packageVersion}, verify it exists and that all sources are available");
            }

            return nuGetPackageInstallResult.PackageDirectory.FullName;
        }

        private async Task<string> GetBaseDirectoryAsync(BootstrapStartOptions startOptions)
        {
            string baseDir;

            if (!string.IsNullOrWhiteSpace(startOptions.BaseDir) && Directory.Exists(startOptions.BaseDir))
            {
                _logger.Information("Using base directory '{BaseDir}' from start options", startOptions.BaseDir);

                baseDir = startOptions.BaseDir;
            }
            else
            {
                baseDir = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());
            }

            return baseDir;
        }

        private async Task<ExitCode> RunBuildToolsAsync(string buildDir, string buildToolDirectoryName)
        {
            var buildToolDirectory = new DirectoryInfo(buildToolDirectoryName);

            List<FileInfo> arborBuild =
                buildToolDirectory.GetFiles("Arbor.Build.dll", SearchOption.TopDirectoryOnly)
                    .Where(file => !file.Name.Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (arborBuild.Count != 1)
            {
                PrintInvalidExeFileCount(arborBuild, buildToolDirectory.FullName);
                return ExitCode.Failure;
            }

            FileInfo buildToolExecutable = arborBuild.Single();

            const string timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            string? timeoutInSecondsFromEnvironment = Environment.GetEnvironmentVariable(timeoutKey);

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

                ImmutableArray<IVariable> variables = await new DotNetEnvironmentVariableProvider()
                    .GetBuildVariablesAsync(
                        _logger,
                        ImmutableArray<IVariable>.Empty,
                        cancellationTokenSource.Token).ConfigureAwait(false);

                string? dotnetExePath = variables.SingleOrDefault(variable =>
                    variable.Key.Equals(WellKnownVariables.DotNetExePath, StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrWhiteSpace(dotnetExePath))
                {
                    _logger.Error("Could not find dotnet.exe");
                    return ExitCode.Failure;
                }

                var arguments = new List<string> {buildToolExecutable.FullName, "--", $"-buildDirectory={buildDir}"};

                if (_startOptions?.Args?.Any() ?? false)
                {
                    arguments.AddRange(_startOptions.Args);
                }

                result = await ProcessRunner.ExecuteProcessAsync(dotnetExePath,
                        arguments,
                        (message, prefix) => _logger.Information("{Prefix}{Message}", buildApplicationPrefix, message),
                        (message, prefix) => _logger.Error("{Prefix}{Message}", buildApplicationPrefix, message),
                        _logger.Information,
                        _logger.Verbose,
                        cancellationToken: cancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }

            return result;
        }

        private void PrintInvalidExeFileCount(List<FileInfo> exeFiles, string buildToolDirectoryPath)
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