using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Git;
using Arbor.X.Core.Tools.Kudu;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Arbor.X.Bootstrapper
{
    public class Bootstrapper
    {
        const int MaxBuildTimeInSeconds = 600;
        static readonly string Prefix = string.Format("[{0}] ", typeof (Bootstrapper).Name);
        readonly ILogger _logger;
        bool _directoryCloneEnabled;
        BootstrapStartOptions _startOptions;

        public Bootstrapper(LogLevel logLevel)
        {
            _logger = new NLogLogger(logLevel, Prefix);
            _logger.Write(string.Format("LogLevel is {0}", logLevel));
        }

        public async Task<ExitCode> StartAsync(string[] args)
        {
            BootstrapStartOptions startOptions;
            if (Debugger.IsAttached)
            {
                startOptions = await StartWithDebuggerAsync(args);
            }
            else
            {
                startOptions = BootstrapStartOptions.Parse(args);
            }

            return await StartAsync(startOptions);
        }

        async Task<BootstrapStartOptions> StartWithDebuggerAsync(string[] args)
        {
            var baseDir = VcsPathHelper.FindVcsRootPath(AppDomain.CurrentDomain.BaseDirectory);

            var tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Arbor.X_Boot_Debug", Guid.NewGuid().ToString()));

            tempDirectory.EnsureExists();

            WriteDebug("Using temp directory '" + tempDirectory + "'");

            await DirectoryCopy.CopyAsync(baseDir, tempDirectory.FullName);

            Environment.SetEnvironmentVariable(WellKnownVariables.BranchNameVersionOverrideEnabled, "true");
            Environment.SetEnvironmentVariable(WellKnownVariables.VariableOverrideEnabled, "true");

            var bootstrapStartOptions = new BootstrapStartOptions(baseDir = tempDirectory.FullName, prereleaseEnabled: true,
                branchName: "refs/heads/develop/12.34.56");

            WriteDebug("Starting with debugger attached");

            return bootstrapStartOptions;
        }

        void WriteDebug(string message)
        {
            Debug.WriteLine(message);
            _logger.WriteDebug(message);
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
                exitCode = await TryStartAsync();
                stopwatch.Stop();
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _logger.WriteError(ex.ToString());

                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    _logger.WriteError(innerEx.ToString());
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _logger.WriteError(ex.ToString());
            }

            ParseResult<int> exitDelayInMilliseconds =
                Environment.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds)
                    .TryParseInt32(0);

            if (exitDelayInMilliseconds > 0)
            {
                _logger.Write(
                    string.Format("Delaying bootstrapper exit with {0} milliseconds as specified in '{1}'",
                        exitDelayInMilliseconds, WellKnownVariables.BootstrapperExitDelayInMilliseconds));
                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds));
            }

            _logger.Write(string.Format("Arbor.X.Bootstrapper total inclusive Arbor.X.Build elapsed time in seconds: {0}",
                stopwatch.Elapsed.TotalSeconds.ToString("F")));

            return exitCode;
        }

        void SetEnvironmentVariables()
        {
            if (!string.IsNullOrWhiteSpace(_startOptions.BaseDir) && Directory.Exists(_startOptions.BaseDir))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.SourceRoot, _startOptions.BaseDir);
            }

            if (_startOptions.PrereleaseEnabled.HasValue)
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.AllowPrerelease, _startOptions.PrereleaseEnabled.Value.ToString().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(_startOptions.BranchName))
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.BranchName, _startOptions.BranchName);
            }
        }

        async Task<ExitCode> TryStartAsync()
        {
            _logger.Write("Starting Arbor.X Bootstrapper");

            string directoryCloneValue = Environment.GetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled);

            _directoryCloneEnabled = directoryCloneValue
                .TryParseBool(defaultValue: true);

            if (!_directoryCloneEnabled)
            {
                _logger.WriteVerbose(string.Format("Environment variable '{0}' has value '{1}'",
                    WellKnownVariables.DirectoryCloneEnabled, directoryCloneValue));
            }

            string baseDir = await GetBaseDirectoryAsync(_startOptions);

            DirectoryInfo buildDir = new DirectoryInfo(Path.Combine(baseDir, "build")).EnsureExists();

            _logger.WriteVerbose(string.Format("Using base directory '{0}'", baseDir));


            string nugetExePath = Path.Combine(buildDir.FullName, "nuget.exe");

            bool nuGetExists = await TryDownloadNuGetAsync(buildDir.FullName, nugetExePath);

            if (!nuGetExists)
            {
                _logger.WriteError(
                    string.Format("NuGet.exe could not be downloaded and it does not already exist at path '{0}'",
                        nugetExePath));
                return ExitCode.Failure;
            }

            string outputDirectoryPath = await DownloadNuGetPackageAsync(buildDir.FullName, nugetExePath);

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                return ExitCode.Failure;
            }

            ExitCode exitCode;
            try
            {
                ExitCode buildToolsResult = await RunBuildToolsAsync(buildDir.FullName, outputDirectoryPath);

                if (buildToolsResult.IsSuccess)
                {
                    _logger.Write("The build tools succeeded");
                }
                else
                {
                    _logger.WriteError(
                        string.Format("The build tools process was not successful, exit code {0}",
                            buildToolsResult));
                }
                exitCode = buildToolsResult;
            }
            catch (TaskCanceledException)
            {
                _logger.WriteError("The build timed out");
                exitCode = ExitCode.Failure;
            }

            return exitCode;
        }

        async Task<string> DownloadNuGetPackageAsync(string buildDir, string nugetExePath)
        {
            const string buildToolPackageName = "Arbor.X";

            string outputDirectoryPath = Path.Combine(buildDir, buildToolPackageName);

            var outputDirectory = new DirectoryInfo(outputDirectoryPath);

            bool reinstall = !outputDirectory.Exists ||
                             Environment.GetEnvironmentVariable(WellKnownVariables.NuGetReinstallArborPackageEnabled)
                                 .TryParseBool(defaultValue: true);

            if (!reinstall)
            {
                return outputDirectoryPath;
            }

            outputDirectory.DeleteIfExists();
            outputDirectory.EnsureExists();

            string version = Environment.GetEnvironmentVariable(WellKnownVariables.ArborXNuGetPackageVersion);

            var nugetArguments = new List<string>
                                 {
                                     "install",
                                     buildToolPackageName,
                                     "-ExcludeVersion",
                                     "-OutputDirectory",
                                     buildDir.TrimEnd('\\'),
                                 };

            if (LogLevel.Verbose.Level <= _logger.LogLevel.Level)
            {
                nugetArguments.Add("-Verbosity");
                nugetArguments.Add("detailed");
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                nugetArguments.Add("-Version");
                nugetArguments.Add(version);

                _logger.WriteVerbose(string.Format("'{0}' flag is set, using specific version of Arbor.X: {1}",
                    WellKnownVariables.ArborXNuGetPackageVersion, version));
            }
            else
            {
                bool allowPrerelease;
                if (_startOptions.PrereleaseEnabled.HasValue)
                {
                    allowPrerelease = _startOptions.PrereleaseEnabled.Value;

                    if (allowPrerelease)
                    {
                        _logger.WriteVerbose(
                            "Prerelease option is set via start options, using latest version of Arbor.X allowing prerelease versions");
                    }
                }
                else
                {
                    allowPrerelease =
                        Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                            .TryParseBool(defaultValue: false);

                    if (allowPrerelease)
                    {
                        _logger.WriteVerbose(
                            string.Format(
                                "'{0}' flag is set, using latest version of Arbor.X allowing prerelease versions",
                                WellKnownVariables.AllowPrerelease));
                    }
                    else
                    {
                        _logger.WriteVerbose(
                            string.Format("'{0}' flag is not set, using latest stable version of Arbor.X",
                                WellKnownVariables.AllowPrerelease));
                    }
                }

                if (allowPrerelease)
                {
                    nugetArguments.Add("-Prerelease");
                }
            }
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxBuildTimeInSeconds));

            ExitCode exitCode =
                await
                    ProcessRunner.ExecuteAsync(nugetExePath, arguments: nugetArguments,
                        cancellationToken: cancellationTokenSource.Token,
                        standardOutLog: _logger.Write,
                        standardErrorAction: _logger.WriteError,
                        toolAction: (message, prefix) => _logger.Write(message),
                        verboseAction: _logger.WriteVerbose);

            if (!exitCode.IsSuccess)
            {
                outputDirectoryPath = string.Empty;
            }

            return outputDirectoryPath;
        }

        async Task<string> GetBaseDirectoryAsync(BootstrapStartOptions startOptions)
        {
            string baseDir;

            if (!string.IsNullOrWhiteSpace(startOptions.BaseDir) && Directory.Exists(startOptions.BaseDir))
            {
                _logger.Write(string.Format("Using base directory '{0}' from start options", startOptions.BaseDir));

                baseDir = startOptions.BaseDir;
            }
            else
            {
                if (IsBetterRunOnLocalTempStorage() && await IsCurrentDirectoryClonableAsync())
                {
                    string clonedDirectory = await CloneDirectoryAsync();

                    baseDir = clonedDirectory;
                }
                else
                {
                    baseDir = VcsPathHelper.FindVcsRootPath();
                }
            }

            return baseDir;
        }

        bool IsBetterRunOnLocalTempStorage()
        {
            bool isKuduAware =
                KuduHelper.IsKuduAware(EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger),
                    _logger);

            bool isBetterRunOnLocalTempStorage = isKuduAware;

            _logger.WriteVerbose("Is Kudu-aware: " + isKuduAware);

            return isBetterRunOnLocalTempStorage;
        }

        async Task<string> CloneDirectoryAsync()
        {
            string targetDirectoryPath = Path.Combine(Path.GetTempPath(), "AX", "R",
                Guid.NewGuid().ToString().Substring(0, 8));

            var targetDirectory = new DirectoryInfo(targetDirectoryPath);

            targetDirectory.EnsureExists();

            string gitExePath = GitHelper.GetGitExePath();

            string sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            IEnumerable<string> cloneArguments = new List<string>
                                                 {
                                                     "clone",
                                                     sourceRoot,
                                                     targetDirectory.FullName
                                                 };


            _logger.WriteVerbose(string.Format("Using temp storage to clone: '{0}'", targetDirectory.FullName));

            ExitCode cloneExitCode =
                await ProcessRunner.ExecuteAsync(gitExePath, arguments: cloneArguments, logger: _logger);

            if (!cloneExitCode.IsSuccess)
            {
                throw new InvalidOperationException(string.Format("Could not clone directory '{0}' to '{1}'", sourceRoot,
                    targetDirectory.FullName));
            }

            return targetDirectory.FullName;
        }

        async Task<bool> IsCurrentDirectoryClonableAsync()
        {
            if (!_directoryCloneEnabled)
            {
                _logger.WriteVerbose("Directory clone is disabled");
                return false;
            }

            _logger.WriteVerbose("Directory clone is enabled");

            string sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                _logger.WriteWarning("Could not find source root");
                return false;
            }

            bool isClonable = false;

            string gitExePath = GitHelper.GetGitExePath();

            if (!string.IsNullOrWhiteSpace(gitExePath))
            {
                string gitDir = Path.Combine(sourceRoot, ".git");

                var statusAllArguments = new[]
                                         {
                                             string.Format("--git-dir={0}", gitDir),
                                             string.Format("--work-tree={0}", sourceRoot), "status"
                                         };

                var argumentVariants = new List<string[]> {new[] {"status"}, statusAllArguments};

                foreach (string[] argumentVariant in argumentVariants)
                {
                    ExitCode statusExitCode = await ProcessRunner.ExecuteAsync(gitExePath,
                        arguments: argumentVariant,
                        standardOutLog: _logger.WriteVerbose,
                        standardErrorAction: _logger.WriteVerbose,
                        toolAction: _logger.Write,
                        verboseAction: _logger.WriteVerbose);

                    if (statusExitCode.IsSuccess)
                    {
                        isClonable = true;
                        break;
                    }
                }
            }

            _logger.WriteVerbose(string.Format("Is directory clonable: {0}", isClonable));

            return isClonable;
        }

        async Task<ExitCode> RunBuildToolsAsync(string buildDir, string buildToolDirectoryName)
        {
            string buildToolDirectoryPath = Path.Combine(buildDir, buildToolDirectoryName);

            var buildToolDirectory = new DirectoryInfo(buildToolDirectoryPath);

            List<FileInfo> exeFiles =
                buildToolDirectory.GetFiles("*.exe", SearchOption.TopDirectoryOnly)
                    .Where(file => file.Name != "nuget.exe")
                    .ToList();

            if (exeFiles.Count != 1)
            {
                PrintInvalidExeFileCount(exeFiles, buildToolDirectoryPath);
                return ExitCode.Failure;
            }

            FileInfo buildToolExe = exeFiles.Single();

            string timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            string timeoutInSecondsFromEnvironment = Environment.GetEnvironmentVariable(timeoutKey);

            ParseResult<int> parseResult =
                timeoutInSecondsFromEnvironment.TryParseInt32(defaultValue: MaxBuildTimeInSeconds);

            if (parseResult.Parsed)
            {
                _logger.WriteVerbose(string.Format("Using timeout from environment variable {0}", timeoutKey));
            }

            int usedTimeoutInSeconds = parseResult;

            _logger.Write(string.Format("Using build timeout {0} seconds", usedTimeoutInSeconds));

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds));

            const string buildApplicationPrefix = "[Arbor.X] ";

            IEnumerable<string> arguments = Enumerable.Empty<string>();
            ExitCode result =
                await
                    ProcessRunner.ExecuteAsync(buildToolExe.FullName,
                        cancellationToken: cancellationTokenSource.Token,
                        arguments: arguments,
                        standardOutLog:
                            (message, prefix) => _logger.Write(message, prefix: buildApplicationPrefix),
                        standardErrorAction:
                            (message, prefix) => _logger.WriteError(message, prefix: buildApplicationPrefix),
                        toolAction: (message, prefix) => _logger.Write(message),
                        verboseAction: _logger.WriteVerbose);

            return result;
        }

        void PrintInvalidExeFileCount(List<FileInfo> exeFiles, string buildToolDirectoryPath)
        {
            string multiple = string.Format("Found {0} such files: {1}", exeFiles.Count,
                string.Join(", ", exeFiles.Select(file => file.Name)));
            const string single = ". Found no such files";
            string found = exeFiles.Any() ? single : multiple;

            _logger.WriteError(
                string.Format("Expected directory {0} to contain exactly one executable file with extensions .exe. {1}",
                    buildToolDirectoryPath, found));
        }

        async Task<bool> TryDownloadNuGetAsync(string baseDir, string targetFile)
        {
            bool update = Environment.GetEnvironmentVariable(WellKnownVariables.NuGetVersionUpdatedEnabled).TryParseBool(defaultValue: false);

            bool hasNugetExe = File.Exists(targetFile);

            try
            {
                if (!hasNugetExe)
                {
                    await DownloadNuGetExeAsync(baseDir, targetFile);
                    update = false;
                }
            }
            catch (HttpRequestException ex)
            {
                if (!File.Exists(targetFile))
                {
                    return false;
                }
                update = true;
                _logger.WriteWarning(
                    string.Format("NuGet.exe could not be downloaded, using existing nuget.exe. {0}", ex));
            }

            if (update)
            {
                try
                {
                    var arguments = new List<string> {"update", "-self"};
                    await ProcessRunner.ExecuteAsync(targetFile, arguments: arguments, logger: _logger);
                }
                catch (Exception ex)
                {
                    _logger.WriteError(ex.ToString());
                }
            }

            bool exists = File.Exists(targetFile);

            return exists;
        }

        async Task DownloadNuGetExeAsync(string baseDir, string targetFile)
        {
            string tempFile = Path.Combine(baseDir, "nuget.exe.tmp");

            const string nugetExeUri = "https://nuget.org/nuget.exe";

            _logger.WriteVerbose(string.Format("Downloading {0} to {1}", nugetExeUri, tempFile));

            using (var client = new HttpClient())
            {
                using (Stream stream = await client.GetStreamAsync(nugetExeUri))
                {
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

                if (File.Exists(tempFile))
                {
                    File.Copy(tempFile, targetFile, overwrite: true);
                    _logger.WriteVerbose(string.Format("Copied {0} to {1}", tempFile, targetFile));
                    File.Delete(tempFile);
                    _logger.WriteVerbose(string.Format("Deleted temp file {0}", tempFile));
                }
            }
        }
    }
}