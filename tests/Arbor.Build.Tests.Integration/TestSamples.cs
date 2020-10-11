﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Logging;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.FS;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;

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
        public async Task Do(string fullPath)
        {
            UPath path = fullPath.AsFullPath();
            var samplesDirectory = GetSamplesDirectory();

            if (samplesDirectory.Path == path)
            {
                _testOutputHelper.WriteLine($"Skipping test for {path}, no samples found starting with _");
                return;
            }

            _testOutputHelper.WriteLine($"Testing {path}");

            var environmentVariables =
                new FallbackEnvironment(new EnvironmentVariables(), new DefaultEnvironmentVariables());

            environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, "develop");
            if (path.FullName.Contains("NoSourceRootDefined"))
            {
                Directory.SetCurrentDirectory(_fs.ConvertPathToInternal(path));
            }
            else
            {
                environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, path.FullName);
            }

            environmentVariables.SetEnvironmentVariable("AllowDebug", "false");

            var xunitLogger = _testOutputHelper.CreateTestLogger();

            var expectedFiles = await GetExpectedFiles(fullPath);

            var logFile = new FileEntry(_fs,UPath.Combine(fullPath, "build.log"));

            logFile.DeleteIfExists();

            var logger = new LoggerConfiguration()
                .WriteTo.Logger(xunitLogger)
                .WriteTo.File(_fs.ConvertPathToInternal(logFile.Path))
                .MinimumLevel.Verbose()
                .CreateLogger();

            using var buildApplication = new BuildApplication(logger, environmentVariables, SpecialFolders.Default, _fs);

            string[] args = Array.Empty<string>();

            var exitCode = await buildApplication.RunAsync(args);

            Assert.Equal(expected: 0, exitCode);

            Assert.All(expectedFiles, file =>
            {
                var filePath = UPath.Combine(fullPath, file);
                Assert.True(_fs.FileExists(filePath), $"Exists({filePath})");
            });

            logger.Dispose();
            xunitLogger.Dispose();
        }

        async Task<ImmutableArray<UPath>> GetExpectedFiles(UPath path)
        {
            var expectedFilesDataPath = UPath.Combine(path, "ExpectedFiles.txt");

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
                yield return new object[] {directoryEntry.FullName};
            }

            if (samplesDirectories.Length == 0)
            {
                yield return new object[] {samplesDirectory.FullName};
            }
        }

        static DirectoryEntry GetSamplesDirectory()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var fs = new PhysicalFileSystem();
#pragma warning restore CA2000 // Dispose objects before losing scope
            var samples = UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "samples");

            var samplesDirectory = fs.GetDirectoryEntry(samples);
            return samplesDirectory;
        }

        public void Dispose() => _fs.Dispose();
    }
}