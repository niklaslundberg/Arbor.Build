using System;
using System.Collections.Generic;
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
        const int MaxBuildTimeInSeconds = 600;
        static readonly string Prefix = string.Format("[{0}] ", typeof (Bootstrapper).Name);
        readonly ConsoleLogger _consoleLogger = new ConsoleLogger(Prefix);
        bool _directoryCloneEnabled;

        public async Task<ExitCode> StartAsync(string[] args)
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

            _consoleLogger.Write(string.Format("Using base directory '{0}'", baseDir));


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

            var nugetArguments = new List<string>
                                 {
                                     "install",
                                     buildToolPackageName,
                                     "-ExcludeVersion",
                                     "-OutputDirectory",
                                     buildDir.TrimEnd('\\'),
                                     "-Verbosity",
                                     "detailed"
                                 };

            var allowPrerelease =
                Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                    .TryParseBool();

            if (allowPrerelease)
            {
                nugetArguments.Add("-Prerelease");
            }
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxBuildTimeInSeconds));

            var exitCode =
                await
                    ProcessRunner.ExecuteAsync(nugetExePath, arguments: nugetArguments,
                        cancellationToken: cancellationTokenSource.Token,
                        standardOutLog: _consoleLogger.Write,
                        standardErrorAction: _consoleLogger.Write,
                        toolAction: message => _consoleLogger.Write(message, ConsoleColor.DarkMagenta));

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


            _consoleLogger.Write(string.Format("Using temp storage to clone: '{0}'", targetDirectory.FullName));

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
                _consoleLogger.Write("Directory clone is disabled");
                return false;
            }

            _consoleLogger.Write("Directory clone is enabled");

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
                    ExitCode statusExitCode = await ProcessRunner.ExecuteAsync(gitExePath, arguments: argumentVariant, standardOutLog: _consoleLogger.Write, standardErrorAction: _consoleLogger.WriteError, toolAction: _consoleLogger.Write);

                    if (statusExitCode.IsSuccess)
                    {
                        isClonable = true;
                        break;
                    }
                }
            }

            _consoleLogger.Write(string.Format("Is directory clonable: {0}", isClonable));

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

            int usedTimeoutInSeconds = MaxBuildTimeInSeconds;
            var timeoutKey = WellKnownVariables.BuildToolTimeoutInSeconds;
            var timeoutInSecondsFromEnvironment = Environment.GetEnvironmentVariable(timeoutKey);

            if (!string.IsNullOrWhiteSpace(timeoutInSecondsFromEnvironment))
            {
                int timeoutInSeconds;
                if (int.TryParse(timeoutInSecondsFromEnvironment, out timeoutInSeconds) && timeoutInSeconds > 0)
                {
                    usedTimeoutInSeconds = timeoutInSeconds;
                    _consoleLogger.Write(string.Format("Using timeout from environment variable {0}", timeoutKey));
                }
            }

            _consoleLogger.Write(string.Format("Using build timeout {0} seconds", usedTimeoutInSeconds));

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(usedTimeoutInSeconds));
            
            IEnumerable<string> arguments = Enumerable.Empty<string>();
            var result =
                await
                    ProcessRunner.ExecuteAsync(buildToolExe.FullName, cancellationToken: cancellationTokenSource.Token,
                        arguments: arguments, standardOutLog: _consoleLogger.Write,
                        standardErrorAction: _consoleLogger.WriteError,
                        toolAction: message => _consoleLogger.Write(message, ConsoleColor.DarkMagenta));

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

                _consoleLogger.Write(string.Format("NuGet.exe could not be downloaded, using existing nuget.exe. {0}", ex));
            }

            return true;
        }

        async Task DownloadNuGetExeAsync(string baseDir, string targetFile)
        {
            var tempFile = Path.Combine(baseDir, "nuget.exe.tmp");

            const string nugetExeUri = "https://nuget.org/nuget.exe";

            _consoleLogger.Write(string.Format("Downloading {0} to {1}", nugetExeUri, tempFile));

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
                    _consoleLogger.Write(string.Format("Copied {0} to {1}", tempFile, targetFile));
                    File.Delete(tempFile);
                    _consoleLogger.Write(string.Format("Deleted temp file {0}", tempFile));
                }
            }
        }
    }
}