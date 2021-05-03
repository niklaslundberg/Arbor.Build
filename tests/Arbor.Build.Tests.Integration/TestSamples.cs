using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.FS;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;
using Assert = Xunit.Assert;

namespace Arbor.Build.Tests.Integration
{
    public sealed class TestSamples : IDisposable
    {
        readonly ITestOutputHelper _testOutputHelper;
        readonly IFileSystem _fs;

        public TestSamples(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _fs = new PhysicalFileSystem();
        }

        [MemberData(nameof(Data))]
        [Theory]
        public async Task RunBuildOnExampleProject(string directoryName)
        {
            if (directoryName == "")
            {
                _testOutputHelper.WriteLine($"Skipping sample tests, no samples found starting with _");
                return;
            }

            var samplesDirectory = GetSamplesDirectory();

            var sampleDirectory = samplesDirectory.GetDirectories(directoryName).Single();

            _testOutputHelper.WriteLine($"Testing {sampleDirectory.ConvertPathToInternal()}");

            var environmentVariables =
                new FallbackEnvironment(new EnvironmentVariables(), new DefaultEnvironmentVariables());

            environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, "develop");
            if (sampleDirectory.FullName.Contains("NoSourceRootDefined", StringComparison.InvariantCulture))
            {
                Directory.SetCurrentDirectory(sampleDirectory.ConvertPathToInternal());
            }
            else
            {
                environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, sampleDirectory.ConvertPathToInternal());
            }

            environmentVariables.SetEnvironmentVariable("AllowDebug", "false");

            using var xunitLogger = _testOutputHelper.CreateTestLogger();

            var expectedFiles = await GetExpectedFiles(sampleDirectory);

            var logFile = new FileEntry(_fs, sampleDirectory.Path / "build.log");

            logFile.DeleteIfExists();

            var invalidPathMessages = new List<string>();

            void FindFilePath(LogEvent obj)
            {
                string? message = obj.RenderMessage();

                if (message.Contains("/mnt/", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("C:/", StringComparison.OrdinalIgnoreCase))
                {
                    invalidPathMessages.Add(message);
                }
            }

            var conditionalLogger = new LoggerConfiguration()
                .WriteTo.Sink(new DelegatingSink(FindFilePath))
                .MinimumLevel.Verbose()
                .CreateLogger();

            using var logger = new LoggerConfiguration()
                .WriteTo.Logger(xunitLogger)
                .WriteTo.File(_fs.ConvertPathToInternal(logFile.Path))
                .MinimumLevel.Verbose()
                .WriteTo.Logger(conditionalLogger)
                .CreateLogger();

            using var buildApplication = new BuildApplication(logger, environmentVariables, SpecialFolders.Default, _fs);

            string[] args = Array.Empty<string>();

            var exitCode = await buildApplication.RunAsync(args);

            Assert.Equal(expected: 0, exitCode);

            Assert.All(expectedFiles, file =>
            {
                var filePath = sampleDirectory.Path / file;
                Assert.True(_fs.FileExists(filePath), $"Exists({_fs.ConvertPathToInternal(filePath)})");

                if (file.GetExtensionWithDot()?.Equals(".nupkg", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    using var packageStream = _fs.OpenFile(filePath, FileMode.Open, FileAccess.Read);

                    DirectoryEntry? tempDir = null;
                    try
                    {
                        UPath tempPath = Path.GetTempPath().ParseAsPath() / Guid.NewGuid().ToString();
                        tempDir = new DirectoryEntry(_fs, tempPath).EnsureExists();

                        var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

                        archive.ExtractToDirectory(tempDir.ConvertPathToInternal());
                    }
                    finally
                    {
                        tempDir.DeleteIfExists();
                    }
                }
            });

            Assert.Empty(invalidPathMessages);
        }

        async Task<ImmutableArray<UPath>> GetExpectedFiles(DirectoryEntry directory)
        {
            var expectedFilesDataPath = UPath.Combine(directory.Path, "ExpectedFiles.txt");

            if (!_fs.FileExists(expectedFilesDataPath))
            {
                return ImmutableArray<UPath>.Empty;
            }

            await using var openFile = _fs.OpenFile(expectedFilesDataPath,FileMode.Open,FileAccess.Read);
            var expectedFiles = await openFile.ReadAllLinesAsync();

            return expectedFiles.Select(expectedPath => new UPath(expectedPath)).ToImmutableArray();
        }

        public static IEnumerable<object[]> Data()
        {
            var samplesDirectory = GetSamplesDirectory();

            var samplesDirectories = samplesDirectory.EnumerateDirectories("_*").ToImmutableArray();

            foreach (var directoryEntry in samplesDirectories)
            {
                yield return new object[] {directoryEntry.Name};
            }

            if (samplesDirectories.Length == 0)
            {
                yield return new object[] {""};
            }
        }

        static DirectoryEntry GetSamplesDirectory()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var fs = new PhysicalFileSystem();
#pragma warning restore CA2000 // Dispose objects before losing scope
            var samples = UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "samples");

            return fs.GetDirectoryEntry(samples);
        }

        public void Dispose() => _fs.Dispose();
    }
}