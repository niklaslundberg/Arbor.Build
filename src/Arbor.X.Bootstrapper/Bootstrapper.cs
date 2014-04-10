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
        const int MaxBuildTimeInMinutes = 15;
        static readonly string Prefix = string.Format("[{0}] ", typeof (Bootstrapper).Name);
        readonly ConsoleLogger _consoleLogger = new ConsoleLogger();

        public async Task<ExitCode> StartAsync(string[] args)
        {
            var baseDir = await GetBaseDirectoryAsync();

            var buildDir = new DirectoryInfo(Path.Combine(baseDir, "build"));

            _consoleLogger.Write(string.Format("Using base directory '{0}'", baseDir));

            string nugetExePath = Path.Combine(buildDir.FullName, "nuget.exe");

            var nuGetExists = await TryDownloadNuGetAsync(buildDir.FullName, nugetExePath);

            if (!nuGetExists)
            {
                Console.Error.WriteLine(Prefix +
                                        "NuGet.exe could not be downloaded and it does not already exist at path '{0}'",
                    nugetExePath);
                return ExitCode.Failure;
            }

            var outputDirectoryPath = await DownloadNuGetPackageAsync(buildDir.FullName, nugetExePath);

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                return ExitCode.Failure;
            }

            var buildToolsResult = await RunBuildToolsAsync(buildDir.FullName, outputDirectoryPath);

            if (!buildToolsResult.IsSuccess)
            {
                Console.Error.WriteLine(Prefix + "The build tools process was not successful, exit code {0}",
                    buildToolsResult);
                return buildToolsResult;
            }

            Console.WriteLine(Prefix + "The build tools succeeded");

            return buildToolsResult;
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
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxBuildTimeInMinutes));

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
            string targetDirectoryPath = Path.Combine(Path.GetTempPath(), "Arbor.X.Kudu.Temp", "repository",
                Guid.NewGuid().ToString());

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

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxBuildTimeInMinutes));


            IEnumerable<string> arguments = Enumerable.Empty<string>();
            var result =
                await
                    ProcessRunner.ExecuteAsync(buildToolExe.FullName, cancellationToken: cancellationTokenSource.Token,
                        arguments: arguments, standardOutLog: _consoleLogger.Write,
                        standardErrorAction: _consoleLogger.WriteError,
                        toolAction: message => _consoleLogger.Write(message, ConsoleColor.DarkMagenta));

            return result;
        }

        static void PrintInvalidExeFileCount(List<FileInfo> exeFiles, string buildToolDirectoryPath)
        {
            string multiple = string.Format("Found {0} such files: {1}", exeFiles.Count,
                string.Join(", ", exeFiles.Select(file => file.Name)));
            const string single = ". Found no such files";
            var found = exeFiles.Any() ? single : multiple;

            Console.Error.WriteLine(Prefix +
                                    "Expected directory {0} to contain exactly one executable file with extensions .exe. {1}",
                buildToolDirectoryPath, found);
        }

        static async Task<bool> TryDownloadNuGetAsync(string baseDir, string targetFile)
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

                Console.WriteLine(Prefix + "NuGet.exe could not be downloaded, using existing nuget.exe. {0}", ex);
            }

            return true;
        }

        static async Task DownloadNuGetExeAsync(string baseDir, string targetFile)
        {
            var tempFile = Path.Combine(baseDir, "nuget.exe.tmp");

            const string nugetExeUri = "https://nuget.org/nuget.exe";

            Console.WriteLine(Prefix + "Downloading {0} to {1}", nugetExeUri, tempFile);

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
                    Console.WriteLine(Prefix + "Copied {0} to {1}", tempFile, targetFile);
                    File.Delete(tempFile);
                    Console.WriteLine(Prefix + "Deleted temp file {0}", tempFile);
                }
            }
        }
    }
}