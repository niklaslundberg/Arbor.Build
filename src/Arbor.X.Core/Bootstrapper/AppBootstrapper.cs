using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Build.Core.GenericExtensions.Int;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.DotNet;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.Kudu;
using Arbor.Exceptions;
using Arbor.Processing;
using Arbor.Tooler;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core.Bootstrapper
{
    public class AppBootstrapper
    {
        private const string BuildToolPackageName = ArborConstants.ArborBuild;
        private const int MaxBuildTimeInSeconds = 600;
        private static readonly string _Prefix = $"[{ArborConstants.ArborBuild}.{nameof(AppBootstrapper)}] ";
        private readonly ILogger _logger;
        private bool _directoryCloneEnabled;

        private bool _failed;
        private BootstrapStartOptions _startOptions;

        public AppBootstrapper(ILogger logger) => _logger = logger;

        public async Task<ExitCode> StartAsync(string[] args)
        {
            _logger.Information("Running Arbor.X Bootstrapper process id {ProcessId}, executable {Executable}",
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
            _startOptions = startOptions ?? new BootstrapStartOptions();

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
                _logger.Error(ex, "{Prefix}", _Prefix);

                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    _logger.Error(innerEx, "{Prefix}", _Prefix);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _logger.Error(ex, "{Prefix} Could not start process", _Prefix);
            }

            _ = Environment.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds)
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
                "Arbor.X.Bootstrapper total inclusive Arbor.X.Build elapsed time in seconds: {ElapsedSeconds}",
                stopwatch.Elapsed.TotalSeconds.ToString("F", CultureInfo.InvariantCulture));

            return exitCode;
        }

        private static void KillAllProcessesSpawnedBy(uint parentProcessId, ILogger logger)
        {
            logger.Debug("Finding processes spawned by process with Id [{ParentProcessId}]", parentProcessId);

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessId={parentProcessId}"))
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
                            using Process childProcess = Process.GetProcessById((int)childProcessId);
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
                tempDirectory.FullName,
                true,
                "refs/heads/develop/12.34.56");

            WriteDebug("Starting with debugger attached");

            return bootstrapStartOptions;
        }

        private void WriteDebug(string message)
        {
            Debug.WriteLine(_Prefix + message);
            _logger.Debug("{Prefix}{Message}", _Prefix, message);
        }

        private void SetEnvironmentVariables()
        {
            if (!string.IsNullOrWhiteSpace(_startOptions.BaseDir) && Directory.Exists(_startOptions.BaseDir))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.SourceRoot, _startOptions.BaseDir);
            }

            if (_startOptions.PreReleaseEnabled.HasValue)
            {
                Environment.SetEnvironmentVariable(
                    WellKnownVariables.AllowPrerelease,
                    _startOptions.PreReleaseEnabled.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(_startOptions.BranchName))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.BranchName, _startOptions.BranchName);
            }
        }

        private async Task<ExitCode> TryStartAsync(BootstrapStartOptions startOptions)
        {
            _logger.Information("Starting Arbor.Build Bootstrapper");

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

            string customNuGetPath =
                Environment.GetEnvironmentVariable(WellKnownVariables.ExternalTools_NuGet_ExePath_Custom);

            string nugetExePath;

            if (!string.IsNullOrWhiteSpace(customNuGetPath) && File.Exists(customNuGetPath))
            {
                nugetExePath = customNuGetPath;
            }
            else
            {
                using var httpClient = new HttpClient();
                var nuGetDownloadClient = new NuGetDownloadClient();
                NuGetDownloadResult nuGetDownloadResult = await nuGetDownloadClient.DownloadNuGetAsync(NuGetDownloadSettings.Default, _logger, httpClient).ConfigureAwait(false);

                if (!nuGetDownloadResult.Succeeded)
                {
                    _logger.Error("Could not download nuget.exe");
                    return ExitCode.Failure;
                }

                nugetExePath = nuGetDownloadResult.NuGetExePath;
            }

            string outputDirectoryPath =
                await DownloadNuGetPackageAsync(buildDir.FullName, nugetExePath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
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
                        await RunBuildToolsAsync(buildDir.FullName, outputDirectoryPath).ConfigureAwait(false);

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
                    _ = Environment.GetEnvironmentVariable("KillSpawnedProcess").TryParseBool(out bool enabled, true);

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

        private async Task<string> DownloadNuGetPackageAsync(string buildDir, string nugetExePath)
        {
            string outputDirectoryPath = Path.Combine(buildDir, BuildToolPackageName);

            var outputDirectory = new DirectoryInfo(outputDirectoryPath);

            _ = Environment.GetEnvironmentVariable(WellKnownVariables.NuGetReinstallArborPackageEnabled)
                .TryParseBool(out bool reinstallEnabled, true);

            bool reinstall = !outputDirectory.Exists || reinstallEnabled;

            if (!reinstall)
            {
                return outputDirectoryPath;
            }

            outputDirectory.DeleteIfExists();
            outputDirectory.EnsureExists();

            string? version = Environment.GetEnvironmentVariable(WellKnownVariables.ArborBuildNuGetPackageVersion);

            string? nuGetSource = Environment.GetEnvironmentVariable(WellKnownVariables.ArborXNuGetPackageSource);

            _ = Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                .TryParseBool(out bool preReleaseIsAllowed);

            preReleaseIsAllowed = _startOptions.PreReleaseEnabled ?? preReleaseIsAllowed;

            if (string.IsNullOrWhiteSpace(version))
            {
                version = await GetLatestVersionAsync(nugetExePath, nuGetSource, preReleaseIsAllowed).ConfigureAwait(false);
            }

            var nugetArguments = new List<string>
            {
                "install",
                BuildToolPackageName,
                "-ExcludeVersion",
                "-OutputDirectory",
                buildDir.TrimEnd('\\')
            };

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                nugetArguments.Add("-Verbosity");
                nugetArguments.Add("detailed");
            }

            if (!string.IsNullOrWhiteSpace(nuGetSource))
            {
                nugetArguments.Add("-Source");
                nugetArguments.Add(nuGetSource);
            }

            string noCache = Environment.GetEnvironmentVariable(WellKnownVariables.ArborXNuGetPackageNoCacheEnabled);

            _ = noCache.TryParseBool(out bool noCacheEnabled);

            if (noCacheEnabled)
            {
                nugetArguments.Add("-NoCache");
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                nugetArguments.Add("-Version");
                nugetArguments.Add(version);

                _logger.Verbose(
                    "'{ArborBuildNuGetPackageVersion}' flag is set, using specific version of Arbor.X: {Version}",
                    WellKnownVariables.ArborBuildNuGetPackageVersion,
                    version);
            }
            else
            {
                _logger.Verbose("'{ArborBuildNuGetPackageVersion}' flag is not set, using latest version of Arbor.X",
                    WellKnownVariables.ArborBuildNuGetPackageVersion);

                bool allowPrerelease;

                if (_startOptions.PreReleaseEnabled.HasValue)
                {
                    allowPrerelease = _startOptions.PreReleaseEnabled.Value;

                    if (allowPrerelease)
                    {
                        _logger.Verbose(
                            "Pre-release option is set via start options, using latest version of Arbor.X allowing prerelease versions");
                    }
                }
                else
                {

                    allowPrerelease = preReleaseIsAllowed;

                    if (allowPrerelease)
                    {
                        _logger.Verbose(
                            "'{AllowPrerelease}' flag is set, using latest version of Arbor.X allowing prerelease versions",
                            WellKnownVariables.AllowPrerelease);
                    }
                    else
                    {
                        _logger.Verbose("'{AllowPrerelease}' flag is not set, using latest stable version of Arbor.X",
                            WellKnownVariables.AllowPrerelease);
                    }
                }

                if (allowPrerelease)
                {
                    nugetArguments.Add("-Prerelease");
                }
            }

            ExitCode exitCode;
            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxBuildTimeInSeconds)))
            {
                exitCode = await ProcessRunner.ExecuteProcessAsync(nugetExePath,
                        arguments: nugetArguments,
                        cancellationToken: cancellationTokenSource.Token,
                        standardOutLog: _logger.Information,
                        standardErrorAction: _logger.Error,
                        toolAction: _logger.Debug,
                        verboseAction: _logger.Verbose)
                    .ConfigureAwait(false);
            }

            if (!exitCode.IsSuccess)
            {
                outputDirectoryPath = string.Empty;
            }

            return outputDirectoryPath;
        }

        private async Task<string?> GetLatestVersionAsync(string nugetExePath, string nuGetSource, bool allowPreRelease)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxBuildTimeInSeconds));
            var nugetArguments = new List<string> { "list", BuildToolPackageName };

            if (!string.IsNullOrWhiteSpace(nuGetSource))
            {
                nugetArguments.Add("-source");
                nugetArguments.Add(nuGetSource);
            }

            if (allowPreRelease)
            {
                nugetArguments.Add("-Prerelease");
            }

            var lines = new List<string>();

            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(nugetExePath,
                    arguments: nugetArguments,
                    cancellationToken: cancellationTokenSource.Token,
                    standardOutLog: (m, _) => lines.Add(m),
                    standardErrorAction: _logger.Error,
                    toolAction: _logger.Debug,
                    verboseAction: _logger.Verbose)
                .ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                return null;
            }

            const string arborBuildPackageName = BuildToolPackageName + " ";
            ImmutableArray<SemanticVersion> matchingPackages =
                lines.Where(line => line.StartsWith(arborBuildPackageName, StringComparison.OrdinalIgnoreCase))
                    .Select(line => line.Replace(arborBuildPackageName, "", StringComparison.OrdinalIgnoreCase))
                    .Select(line => (SemanticVersion.TryParse(line, out SemanticVersion semVer), semVer))
                    .Where(s => s.Item1)
                    .Select(s => s.semVer)
                    .ToImmutableArray();

            if (matchingPackages.Length == 0)
            {
                return null;
            }

            if (matchingPackages.Length == 1)
            {
                return matchingPackages.Single().ToNormalizedString();
            }

            return matchingPackages.Max().ToNormalizedString();
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
                if (IsBetterRunOnLocalTempStorage() && await IsCurrentDirectoryClonableAsync().ConfigureAwait(false))
                {
                    string clonedDirectory = await CloneDirectoryAsync().ConfigureAwait(false);

                    baseDir = clonedDirectory;
                }
                else
                {
                    baseDir = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());
                }
            }

            return baseDir;
        }

        private bool IsBetterRunOnLocalTempStorage()
        {
            bool isKuduAware = KuduHelper.IsKuduAware(
                EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger),
                _logger);

            bool isBetterRunOnLocalTempStorage = isKuduAware;

            _logger.Verbose("Is Kudu-aware: {IsKuduAware}", isKuduAware);

            return isBetterRunOnLocalTempStorage;
        }

        private async Task<string> CloneDirectoryAsync()
        {
            string targetDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                DefaultPaths.TempPathPrefix,
                "R",
                Guid.NewGuid().ToString().Substring(0, 8));

            var targetDirectory = new DirectoryInfo(targetDirectoryPath);

            targetDirectory.EnsureExists();

            string gitExePath = GitHelper.GetGitExePath(_logger);

            string sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            IEnumerable<string> cloneArguments = new List<string>
            {
                "clone",
                sourceRoot,
                targetDirectory.FullName
            };

            _logger.Verbose("Using temp storage to clone: '{FullName}'", targetDirectory.FullName);

            ExitCode cloneExitCode = await ProcessHelper.ExecuteAsync(
                gitExePath,
                cloneArguments,
                _logger,
                addProcessNameAsLogCategory: true,
                addProcessRunnerCategory: true,
                parentPrefix: _Prefix).ConfigureAwait(false);

            if (!cloneExitCode.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Could not clone directory '{sourceRoot}' to '{targetDirectory.FullName}'");
            }

            return targetDirectory.FullName;
        }

        private async Task<bool> IsCurrentDirectoryClonableAsync()
        {
            if (!_directoryCloneEnabled)
            {
                _logger.Verbose("Directory clone is disabled");
                return false;
            }

            _logger.Verbose("Directory clone is enabled");

            string sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                _logger.Warning("Could not find source root");
                return false;
            }

            bool isClonable = false;

            string gitExePath = GitHelper.GetGitExePath(_logger);

            if (!string.IsNullOrWhiteSpace(gitExePath))
            {
                string gitDir = Path.Combine(sourceRoot, ".git");

                string[] statusAllArguments =
                {
                    $"--git-dir={gitDir}",
                    $"--work-tree={sourceRoot}",
                    "status"
                };

                var argumentVariants = new List<string[]> { new[] { "status" }, statusAllArguments };

                foreach (string[] argumentVariant in argumentVariants)
                {
                    ExitCode statusExitCode = await ProcessRunner.ExecuteProcessAsync(
                        gitExePath,
                        arguments: argumentVariant,
                        standardOutLog: _logger.Verbose,
                        standardErrorAction: _logger.Verbose,
                        toolAction: _logger.Information,
                        verboseAction: _logger.Verbose).ConfigureAwait(false);

                    if (statusExitCode.IsSuccess)
                    {
                        isClonable = true;
                        break;
                    }
                }
            }

            _logger.Verbose("Is directory clonable: {IsClonable}", isClonable);

            return isClonable;
        }

        private async Task<ExitCode> RunBuildToolsAsync(string buildDir, string buildToolDirectoryName)
        {
            string buildToolDirectoryPath = Path.Combine(buildDir, buildToolDirectoryName);

            var buildToolDirectory = new DirectoryInfo(buildToolDirectoryPath);

            List<FileInfo> arborBuild =
                buildToolDirectory.GetFiles("Arbor.Build.dll", SearchOption.TopDirectoryOnly)
                    .Where(file => !file.Name.Equals("nuget.exe", StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (arborBuild.Count != 1)
            {
                PrintInvalidExeFileCount(arborBuild, buildToolDirectoryPath);
                return ExitCode.Failure;
            }

            FileInfo buildToolExecutable = arborBuild.Single();

            const string timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            string timeoutInSecondsFromEnvironment = Environment.GetEnvironmentVariable(timeoutKey);

            if (timeoutInSecondsFromEnvironment.TryParseInt32(out int parseResult, MaxBuildTimeInSeconds))
            {
                _logger.Verbose("Using timeout from environment variable {TimeoutKey}", timeoutKey);
            }

            int usedTimeoutInSeconds = parseResult;

            _logger.Information("Using build timeout {UsedTimeoutInSeconds} seconds", usedTimeoutInSeconds);

            ExitCode result;
            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds)))
            {
                const string buildApplicationPrefix = "[Arbor.Build] ";

                ImmutableArray<IVariable> variables = await new DotNetEnvironmentVariableProvider().GetBuildVariablesAsync(
                    _logger,
                    ImmutableArray<IVariable>.Empty,
                    cancellationTokenSource.Token).ConfigureAwait(false);

                string dotnetExePath = variables.SingleOrDefault(variable =>
                    variable.Key.Equals(WellKnownVariables.DotNetExePath, StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrWhiteSpace(dotnetExePath))
                {
                    _logger.Error("Could not find dotnet.exe");
                    return ExitCode.Failure;
                }

                string[] arguments = { buildToolExecutable.FullName };

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
            const string Single = ". Found no such files";
            string found = exeFiles.Count > 0 ? Single : multiple;

            _logger.Error(
                "Expected directory {BuildToolDirectoryPath} to contain exactly one executable file with extensions .exe. {Found}",
                buildToolDirectoryPath,
                found);
        }
    }
}
