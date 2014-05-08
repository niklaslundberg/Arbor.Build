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
using Arbor.X.Core.Tools.Git;
using Arbor.X.Core.Tools.Kudu;

namespace Arbor.X.Bootstrapper
{
    public class Bootstrapper
    {
        public Bootstrapper(LogLevel logLevel)
        {
            _consoleLogger = new ConsoleLogger(Prefix, logLevel);
            _consoleLogger.Write(string.Format("LogLevel is {0}", logLevel));
        }
        const int MaxBuildTimeInSeconds = 600;
        static readonly string Prefix = string.Format("[{0}] ", typeof (Bootstrapper).Name);
        readonly ConsoleLogger _consoleLogger;
        bool _directoryCloneEnabled;

        public async Task<ExitCode> StartAsync(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            ExitCode exitCode;
            try
            {
                exitCode = await TryStartAsync(args);
                stopwatch.Stop();
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _consoleLogger.WriteError(ex.ToString());

                foreach (var innerEx in ex.InnerExceptions)
                {
                    _consoleLogger.WriteError(innerEx.ToString());
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                exitCode = ExitCode.Failure;
                _consoleLogger.WriteError(ex.ToString());
            }

            var exitDelayInMilliseconds =
                Environment.GetEnvironmentVariable(WellKnownVariables.BootstrapperExitDelayInMilliseconds).TryParseInt32(0);

            if (exitDelayInMilliseconds > 0)
            {
                _consoleLogger.Write(string.Format("Delaying bootstrapper exit with {0} milliseconds as specified in '{1}'", exitDelayInMilliseconds, WellKnownVariables.BootstrapperExitDelayInMilliseconds));
                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds));
            }

            _consoleLogger.Write("Arbor.X.Bootstrapper total inclusive Arbor.X.Build elapsed time in seconds: {0}", stopwatch.Elapsed.TotalSeconds.ToString("F"));

            return exitCode;
        }

        public async Task<ExitCode> TryStartAsync(string[] args)
        {
            _consoleLogger.Write("Starting Arbor.X Bootstrapper");

            var directoryCloneValue = Environment.GetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled);

            _directoryCloneEnabled = directoryCloneValue
                .TryParseBool(defaultValue: true);

            if (!_directoryCloneEnabled)
            {
                _consoleLogger.WriteVerbose(string.Format("Environment variable '{0}' has value '{1}'", WellKnownVariables.DirectoryCloneEnabled, directoryCloneValue));
            }

            var baseDir = await GetBaseDirectoryAsync();

            var buildDir = new DirectoryInfo(Path.Combine(baseDir, "build"));

            _consoleLogger.WriteVerbose(string.Format("Using base directory '{0}'", baseDir));


            string nugetExePath = Path.Combine(buildDir.FullName, "nuget.exe");

            var nuGetExists = await TryDownloadNuGetAsync(buildDir.FullName, nugetExePath);

            if (!nuGetExists)
            {
                _consoleLogger.WriteError(string.Format("NuGet.exe could not be downloaded and it does not already exist at path '{0}'",
                    nugetExePath));
                return ExitCode.Failure;
            }

            var outputDirectoryPath = await DownloadNuGetPackageAsync(buildDir.FullName, nugetExePath);

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
                    _consoleLogger.Write("The build tools succeeded");
                }
                else
                {
                    _consoleLogger.WriteError(
                        string.Format("The build tools process was not successful, exit code {0}",
                            buildToolsResult));
                }
                exitCode = buildToolsResult;
            }
            catch (TaskCanceledException)
            {
                _consoleLogger.WriteError("The build timed out");
                exitCode = ExitCode.Failure;
            }

            return exitCode;
        }

        async Task<string> DownloadNuGetPackageAsync(string buildDir, string nugetExePath)
        {
            const string buildToolPackageName = "Arbor.X";

            string outputDirectoryPath = Path.Combine(buildDir, buildToolPackageName);

            var outputDirectory = new DirectoryInfo(outputDirectoryPath);

            outputDirectory.DeleteIfExists();
            outputDirectory.EnsureExists();

            var version = Environment.GetEnvironmentVariable(WellKnownVariables.ArborXNuGetPackageVersion);

            var nugetArguments = new List<string>
                                 {
                                     "install",
                                     buildToolPackageName,
                                     "-ExcludeVersion",
                                     "-OutputDirectory",
                                     buildDir.TrimEnd('\\'),
                                 };

            if (LogLevel.Verbose.Level <= _consoleLogger.LogLevel.Level)
            {
                nugetArguments.Add("-Verbosity");
                nugetArguments.Add("detailed");
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                nugetArguments.Add("-Version");
                nugetArguments.Add(version);

                _consoleLogger.WriteVerbose(string.Format("'{0}' flag is set, using specific version of Arbor.X: {1}", WellKnownVariables.ArborXNuGetPackageVersion, version));
            }
            else
            {
                var allowPrerelease =
                    Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                        .TryParseBool(defaultValue: false);

                if (allowPrerelease)
                {
                    _consoleLogger.WriteVerbose(string.Format("'{0}' flag is set, using latest version of Arbor.X allowing prerelease versions", WellKnownVariables.AllowPrerelease));
                    nugetArguments.Add("-Prerelease");
                }
                else
                {
                    _consoleLogger.WriteVerbose(string.Format("'{0}' flag is not set, using latest stable version of Arbor.X", WellKnownVariables.AllowPrerelease));
                }
            }
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxBuildTimeInSeconds));

            var exitCode =
                await
                    ProcessRunner.ExecuteAsync(nugetExePath, arguments: nugetArguments,
                        cancellationToken: cancellationTokenSource.Token,
                        standardOutLog: _consoleLogger.Write,
                        standardErrorAction: _consoleLogger.WriteError,
                        toolAction: (message, prefix) => _consoleLogger.Write(message, ConsoleColor.DarkMagenta),
                        verboseAction: _consoleLogger.WriteVerbose);

            if (!exitCode.IsSuccess)
            {
                outputDirectoryPath = string.Empty;
            }

            return outputDirectoryPath;
        }

        async Task<string> GetBaseDirectoryAsync()
        {
            string baseDir;

            if (IsBetterRunOnLocalTempStorage() && await IsCurrentDirectoryClonableAsync())
            {
                var clonedDirectory = await CloneDirectoryAsync();

                baseDir = clonedDirectory;
            }
            else
            {
                baseDir = VcsPathHelper.FindVcsRootPath();
            }

            return baseDir;
        }

        bool IsBetterRunOnLocalTempStorage()
        {
            var isKuduAware = KuduHelper.IsKuduAware(EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(), _consoleLogger);

            bool isBetterRunOnLocalTempStorage = isKuduAware;
            
            _consoleLogger.WriteVerbose("Is Kudu-aware: " + isKuduAware);
           
            return isBetterRunOnLocalTempStorage;
        }

        async Task<string> CloneDirectoryAsync()
        {
            string targetDirectoryPath = Path.Combine(Path.GetTempPath(), "AX", "R",
                Guid.NewGuid().ToString().Substring(0,8));

            var targetDirectory = new DirectoryInfo(targetDirectoryPath);

            targetDirectory.EnsureExists();

            var gitExePath = GitHelper.GetGitExePath();

            var sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            IEnumerable<string> cloneArguments = new List<string>
                                                 {
                                                     "clone",
                                                     sourceRoot,
                                                     targetDirectory.FullName
                                                 };


            _consoleLogger.WriteVerbose(string.Format("Using temp storage to clone: '{0}'", targetDirectory.FullName));

            ExitCode cloneExitCode = await ProcessRunner.ExecuteAsync(gitExePath, arguments: cloneArguments);

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
                _consoleLogger.WriteVerbose("Directory clone is disabled");
                return false;
            }

            _consoleLogger.WriteVerbose("Directory clone is enabled");

            var sourceRoot = VcsPathHelper.TryFindVcsRootPath();

            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                _consoleLogger.WriteWarning("Could not find source root");
                return false;
            }

            bool isClonable = false;

            var gitExePath = GitHelper.GetGitExePath();

            if (!string.IsNullOrWhiteSpace(gitExePath))
            {
                string gitDir = Path.Combine(sourceRoot, ".git");

                var statusAllArguments = new[] { string.Format("--git-dir={0}", gitDir), string.Format("--work-tree={0}", sourceRoot), "status" };

                var argumentVariants = new List<string[]> { new[]{"status"}, statusAllArguments };

                foreach (var argumentVariant in argumentVariants)
                {
                    ExitCode statusExitCode = await ProcessRunner.ExecuteAsync(gitExePath, 
                        arguments: argumentVariant, 
                        standardOutLog: _consoleLogger.WriteVerbose, 
                        standardErrorAction: _consoleLogger.WriteVerbose, 
                        toolAction: _consoleLogger.Write,
                        verboseAction: _consoleLogger.WriteVerbose);

                    if (statusExitCode.IsSuccess)
                    {
                        isClonable = true;
                        break;
                    }
                }
            }

            _consoleLogger.WriteVerbose(string.Format("Is directory clonable: {0}", isClonable));

            return isClonable;
        }

        async Task<ExitCode> RunBuildToolsAsync(string buildDir, string buildToolDirectoryName)
        {
            var buildToolDirectoryPath = Path.Combine(buildDir, buildToolDirectoryName);

            var buildToolDirectory = new DirectoryInfo(buildToolDirectoryPath);

            var exeFiles =
                buildToolDirectory.GetFiles("*.exe", SearchOption.TopDirectoryOnly)
                    .Where(file => file.Name != "nuget.exe")
                    .ToList();

            if (exeFiles.Count != 1)
            {
                PrintInvalidExeFileCount(exeFiles, buildToolDirectoryPath);
                return ExitCode.Failure;
            }

            var buildToolExe = exeFiles.Single();

            var timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            var timeoutInSecondsFromEnvironment = Environment.GetEnvironmentVariable(timeoutKey);

            var parseResult = timeoutInSecondsFromEnvironment.TryParseInt32(defaultValue:MaxBuildTimeInSeconds);

            if (parseResult.Parsed)
            {
                _consoleLogger.WriteVerbose(string.Format("Using timeout from environment variable {0}", timeoutKey));
            }

            int usedTimeoutInSeconds = parseResult;

            _consoleLogger.Write(string.Format("Using build timeout {0} seconds", usedTimeoutInSeconds));

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds));

            const string buildApplicationPrefix = "[Arbor.X] ";

            IEnumerable<string> arguments = Enumerable.Empty<string>();
            var result =
                await
                    ProcessRunner.ExecuteAsync(buildToolExe.FullName,
                        cancellationToken: cancellationTokenSource.Token,
                        arguments: arguments,
                        standardOutLog:
                            (message, prefix) => _consoleLogger.Write(message, prefix: buildApplicationPrefix),
                        standardErrorAction:
                            (message, prefix) => _consoleLogger.WriteError(message, prefix: buildApplicationPrefix),
                        toolAction: (message, prefix) => _consoleLogger.Write(message, ConsoleColor.DarkMagenta),
                        verboseAction: _consoleLogger.WriteVerbose);

            return result;
        }

        void PrintInvalidExeFileCount(List<FileInfo> exeFiles, string buildToolDirectoryPath)
        {
            string multiple = string.Format("Found {0} such files: {1}", exeFiles.Count,
                string.Join(", ", exeFiles.Select(file => file.Name)));
            const string single = ". Found no such files";
            var found = exeFiles.Any() ? single : multiple;

            _consoleLogger.WriteError(string.Format("Expected directory {0} to contain exactly one executable file with extensions .exe. {1}",
                buildToolDirectoryPath, found));
        }

        async Task<bool> TryDownloadNuGetAsync(string baseDir, string targetFile)
        {
            try
            {
                await DownloadNuGetExeAsync(baseDir, targetFile);
            }
            catch (HttpRequestException ex)
            {
                if (!File.Exists(targetFile))
                {
                    return false;
                }

                _consoleLogger.WriteWarning(string.Format("NuGet.exe could not be downloaded, using existing nuget.exe. {0}", ex));
            }

            return true;
        }

        async Task DownloadNuGetExeAsync(string baseDir, string targetFile)
        {
            var tempFile = Path.Combine(baseDir, "nuget.exe.tmp");

            const string nugetExeUri = "https://nuget.org/nuget.exe";

            _consoleLogger.WriteVerbose(string.Format("Downloading {0} to {1}", nugetExeUri, tempFile));

            using (var client = new HttpClient())
            {
                using (var stream = await client.GetStreamAsync(nugetExeUri))
                {
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

                if (File.Exists(tempFile))
                {
                    File.Copy(tempFile, targetFile, overwrite: true);
                    _consoleLogger.WriteVerbose(string.Format("Copied {0} to {1}", tempFile, targetFile));
                    File.Delete(tempFile);
                    _consoleLogger.WriteVerbose(string.Format("Deleted temp file {0}", tempFile));
                }
            }
        }
    }
}