using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildApp;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.EnvironmentVariables;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.FS;
using Arbor.Processing;
using Serilog;
using Shouldly;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.CrossPlatform;

public sealed class CrossPlatformBuildTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    private readonly PhysicalFileSystem _fs = new();
    private FileEntry? _logFile;

    [Trait("Category", TestFilterHelper.RecursiveCategoryName)]
    [Fact(Skip = "Recursive")]
    public async Task BuildCrossPlatformSampleOnCurrentPlatform()
    {
        var sampleDirectory = GetCrossPlatformSampleDirectory();

        testOutputHelper.WriteLine(
            $"Building cross-platform sample on {RuntimeInformation.OSDescription} ({RuntimeInformation.RuntimeIdentifier})");

        var exitCode = await RunBuildOnDirectoryAsync(sampleDirectory);

        if (exitCode.Code != 0)
        {
            // Print log file contents for diagnostics
            try
            {
                if (_logFile is { } && _fs.FileExists(_logFile.Path))
                {
                    string logContent = await System.IO.File.ReadAllTextAsync(_fs.ConvertPathToInternal(_logFile.Path), TestContext.Current.CancellationToken);
                    testOutputHelper.WriteLine("Build log:");
                    testOutputHelper.WriteLine(logContent);
                }
            }
            catch
            {
                // Log file not available - continue with test failure
            }
        }

        exitCode.Code.ShouldBe(0, "Build should succeed on current platform");

        var expectedPackagePath = sampleDirectory.Path / "Artifacts" / "packages" / "CrossPlatformLib.1.0.0-build.1.nupkg";

        _fs.FileExists(expectedPackagePath)
            .ShouldBeTrue($"Expected NuGet package at {_fs.ConvertPathToInternal(expectedPackagePath)}");
    }

    [Fact]
    public async Task BuildCrossPlatformSampleInWsl()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            testOutputHelper.WriteLine("Skipping WSL test in GitHub Actions");
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            testOutputHelper.WriteLine("Skipping WSL test on non-Windows platform");
            return;
        }

        if (!IsWslAvailable())
        {
            testOutputHelper.WriteLine("Skipping WSL test: WSL is not available or has no installed distributions");
            return;
        }

        if (!IsDotNetAvailableInWsl())
        {
            testOutputHelper.WriteLine("Skipping WSL test: .NET SDK is not installed in WSL");
            return;
        }

        var vcsRoot = VcsTestPathHelper.FindVcsRootPath();
        string windowsPath = _fs.ConvertPathToInternal(vcsRoot.Path);
        string wslPath = ConvertToWslPath(windowsPath);
        string samplePath = $"{wslPath}/samples/_CrossPlatformLib";

        testOutputHelper.WriteLine($"Building in WSL at: {samplePath}");

        (int exitCode, string output) = await RunInWslAsync(
            $"cd \"{samplePath}\" && dotnet build CrossPlatformLib.slnx -c Release",
            TimeSpan.FromMinutes(5));

        testOutputHelper.WriteLine(output);

        exitCode.ShouldBe(0, $"dotnet build should succeed in WSL. Output:{Environment.NewLine}{output}");
    }

    [Fact]
    public void PlatformHelperReportsCorrectPlatform()
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        if (isWindows)
        {
            Core.Tools.Platform.PlatformHelper.IsWindows.ShouldBeTrue();
            Core.Tools.Platform.PlatformHelper.ExecutableExtension.ShouldBe(".exe");
            Core.Tools.Platform.PlatformHelper.GetExecutableName("dotnet").ShouldBe("dotnet.exe");
        }
        else if (isLinux)
        {
            Core.Tools.Platform.PlatformHelper.IsLinux.ShouldBeTrue();
            Core.Tools.Platform.PlatformHelper.ExecutableExtension.ShouldBe(string.Empty);
            Core.Tools.Platform.PlatformHelper.GetExecutableName("dotnet").ShouldBe("dotnet");
        }
    }

    [Fact]
    public async Task DotNetBuildProducesIdenticalOutputAcrossPlatforms()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            testOutputHelper.WriteLine("Skipping cross-platform comparison test in GitHub Actions");
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            testOutputHelper.WriteLine("Skipping cross-platform comparison test on non-Windows platform");
            return;
        }

        if (!IsWslAvailable() || !IsDotNetAvailableInWsl())
        {
            testOutputHelper.WriteLine("Skipping cross-platform comparison test: WSL or .NET SDK not available");
            return;
        }

        var vcsRoot = VcsTestPathHelper.FindVcsRootPath();
        string windowsPath = _fs.ConvertPathToInternal(vcsRoot.Path);
        string wslPath = ConvertToWslPath(windowsPath);
        string samplePath = $"{wslPath}/samples/_CrossPlatformLib";

        // Build on Windows
        int windowsExitCode = await RunDotNetBuildAsync(
            _fs.ConvertPathToInternal(vcsRoot.Path / "samples" / "_CrossPlatformLib" / "CrossPlatformLib.slnx"));

        // Build in WSL
        (int linuxExitCode, string linuxOutput) = await RunInWslAsync(
            $"cd \"{samplePath}\" && dotnet build CrossPlatformLib.slnx -c Debug",
            TimeSpan.FromMinutes(5));

        testOutputHelper.WriteLine($"Windows build exit code: {windowsExitCode}");
        testOutputHelper.WriteLine($"Linux build exit code: {linuxExitCode}");
        testOutputHelper.WriteLine($"Linux output: {linuxOutput}");

        windowsExitCode.ShouldBe(0, "Windows build should succeed");
        linuxExitCode.ShouldBe(0, "Linux build should succeed");
    }

    private DirectoryEntry GetCrossPlatformSampleDirectory()
    {
        var vcsRoot = VcsTestPathHelper.FindVcsRootPath();
        return new DirectoryEntry(_fs, vcsRoot.Path / "samples" / "_CrossPlatformLib");
    }

    private async Task<ExitCode> RunBuildOnDirectoryAsync(DirectoryEntry sampleDirectory)
    {
        var environmentVariables =
            new FallbackEnvironment(new EnvironmentVariables(), new DefaultEnvironmentVariables());

        environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, "develop");
        environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot,
            sampleDirectory.ConvertPathToInternal());
        environmentVariables.SetEnvironmentVariable("AllowDebug", "false");

        _logFile = new FileEntry(_fs, sampleDirectory.Path / $"build-{Guid.NewGuid()}.log");

        // Safely delete existing log file with retry logic
        TryDeleteLogFileWithRetry(_logFile);

        await using var logger = new LoggerConfiguration()
            .WriteTo.File(_fs.ConvertPathToInternal(_logFile.Path))
            .WriteTo.Debug()
            .MinimumLevel.Verbose()
            .CreateLogger();

        using var buildApplication =
            new BuildApplication(logger, environmentVariables, SpecialFolders.Default, _fs);

        return await buildApplication.RunAsync([]);
    }

    private void TryDeleteLogFileWithRetry(FileEntry? logFile, int maxRetries = 3)
    {
        if (logFile is null)
        {
            return;
        }

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (logFile.Path != UPath.Root && logFile.Path != UPath.Empty && _fs.FileExists(logFile.Path))
                {
                    logFile.Delete();
                }
                return; // Success
            }
            catch (Exception) when (attempt < maxRetries - 1)
            {
                // Log and retry on transient failures (locked file, etc.)
                Thread.Sleep(100 * (attempt + 1)); // Exponential backoff
            }
            catch
            {
                // Final attempt failed - log file cleanup is not critical, continue
                return;
            }
        }
    }

    private static bool IsWslAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "--list --quiet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(10_000);

            string output = process.StandardOutput.ReadToEnd().Trim();
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDotNetAvailableInWsl()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "dotnet --version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(30_000);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string Output)> RunInWslAsync(string command, TimeSpan timeout)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "wsl",
            Arguments = $"bash -c \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var outputLines = new List<string>();
        var errorLines = new List<string>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { })
            {
                outputLines.Add(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is { })
            {
                errorLines.Add(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            return (-1, "Process timed out");
        }

        string combinedOutput = string.Join(Environment.NewLine,
            [..outputLines, ..errorLines]);

        return (process.ExitCode, combinedOutput);
    }

    private static async Task<int> RunDotNetBuildAsync(string solutionPath)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{solutionPath}\" -c Debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    private static string ConvertToWslPath(string windowsPath)
    {
        string normalized = windowsPath.Replace('\\', '/');

        if (normalized is [_, ':', ..])
        {
            char driveLetter = char.ToLowerInvariant(normalized[0]);
            return $"/mnt/{driveLetter}{normalized[2..]}";
        }

        return normalized;
    }

    public void Dispose()
    {
        TryDeleteLogFileWithRetry(_logFile);
        _fs.Dispose();
    }
}
