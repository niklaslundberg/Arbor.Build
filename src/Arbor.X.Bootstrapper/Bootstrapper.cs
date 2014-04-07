using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

namespace Arbor.X.Bootstrapper
{
    public class Bootstrapper
    {
        const int MaxBuildTimeInMinutes = 15;
        readonly ConsoleLogger _consoleLogger = new ConsoleLogger();
        static readonly string Prefix = string.Format("[{0}] ", typeof (Bootstrapper).Name);

        public async Task<ExitCode> StartAsync(string[] args)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string nugetExePath = Path.Combine(baseDir, "nuget.exe");

            var nuGetExists = await TryDownloadNuGetAsync(baseDir, nugetExePath);

            if (!nuGetExists)
            {
                Console.Error.WriteLine(Prefix +
                    "NuGet.exe could not be downloaded and it does not already exist at path '{0}'", nugetExePath);
                return ExitCode.Failure;
            }

            const string buildToolPackageName = "Arbor.X";

            string outputDirectoryPath = Path.Combine(baseDir, buildToolPackageName);

            var outputDirectory = new DirectoryInfo(outputDirectoryPath);

            outputDirectory.DeleteIfExists();
            outputDirectory.EnsureExists();

            var nugetArguments = new List<string>
                                 {
                                     "install", buildToolPackageName, "-ExcludeVersion", "-OutputDirectory", baseDir.TrimEnd('\\'),
                                     "-Verbosity", "detailed"
                                 };
            
            var allowPrerelease =
                Environment.GetEnvironmentVariable(WellKnownVariables.AllowPrerelease)
                    .TryParseBool(defaultValue: false);

            if (allowPrerelease)
            {
                nugetArguments.Add("-Prerelease");
            }
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxBuildTimeInMinutes));

            var exitCode = await ProcessRunner.ExecuteAsync(nugetExePath, arguments: nugetArguments, cancellationToken: cancellationTokenSource.Token,
                        standardOutLog: _consoleLogger.Write,
                        standardErrorAction: _consoleLogger.Write,
                        toolAction: message => _consoleLogger.Write(message, ConsoleColor.DarkMagenta));

            if (!exitCode.IsSuccess)
            {
                return exitCode;
            }

            var buildToolsResult = await RunBuildToolsAsync(baseDir, outputDirectoryPath);

            if (!buildToolsResult.IsSuccess)
            {
                Console.Error.WriteLine(Prefix + "The build tools process was not successful, exit code {0}", buildToolsResult);
                return buildToolsResult;
            }

            Console.WriteLine(Prefix + "The build tools succeeded");

            return buildToolsResult;
        }

        async Task<ExitCode> RunBuildToolsAsync(string baseDir, string buildToolDirectoryName)
        {
            var buildToolDirectoryPath = Path.Combine(baseDir, buildToolDirectoryName);

            var buildToolDirectory = new DirectoryInfo(buildToolDirectoryPath);

            var exeFiles = buildToolDirectory.GetFiles("*.exe", SearchOption.TopDirectoryOnly).Where(file => file.Name != "nuget.exe").ToList();

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
                        standardErrorAction: _consoleLogger.Write,
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

            Console.WriteLine(Prefix +"Downloading {0} to {1}", nugetExeUri, tempFile);

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
                    Console.WriteLine(Prefix +"Copied {0} to {1}", tempFile, targetFile);
                    File.Delete(tempFile);
                    Console.WriteLine(Prefix +"Deleted temp file {0}", tempFile);
                }
            }
        }
    }
}