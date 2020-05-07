using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.Build.Tests.Integration
{
    public class TestSamples
    {
        readonly ITestOutputHelper _testOutputHelper;

        public TestSamples(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [MemberData(nameof(Data))]
        [Theory]
        public async Task Do(string path)
        {
            _testOutputHelper.WriteLine("Testing " + path);

            var environmentVariables =
                new FallbackEnvironment(new EnvironmentVariables(), new DefaultEnvironmentVariables());

            environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, "develop");
            environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, path);
            environmentVariables.SetEnvironmentVariable("AllowDebug", "false");

            var xunitLogger = _testOutputHelper.CreateTestLogger();

            var expectedFiles = await GetExpectedFiles(path);

            string logFile = Path.Combine(path, "build.log");

            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }

            var logger = new LoggerConfiguration()
                .WriteTo.Logger(xunitLogger)
                .WriteTo.File(logFile)
                .MinimumLevel.Verbose()
                .CreateLogger();

            var buildApplication = new BuildApplication(logger, environmentVariables, SpecialFolders.Default);

            var args = Array.Empty<string>();

            var exitCode = await buildApplication.RunAsync(args);

            Assert.Equal(expected: 0, exitCode);

            Assert.All(expectedFiles, file =>
            {
                string filePath = Path.Combine(path, file);
                Assert.True(File.Exists(filePath), $"File.Exists({filePath})");
            });
        }

        async Task<ImmutableArray<string>> GetExpectedFiles(string path)
        {
            string expectedFilesDataPath = Path.Combine(path, "ExpectedFiles.txt");

            if (!File.Exists(expectedFilesDataPath))
            {
                return ImmutableArray<string>.Empty;
            }

            var expectedFiles = await File.ReadAllLinesAsync(expectedFilesDataPath);

            return expectedFiles.ToImmutableArray();
        }

        public static IEnumerable<object[]> Data()
        {
            string samples = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "samples");

            var samplesDirectory = new DirectoryInfo(samples);

            foreach (var directoryInfo in samplesDirectory.GetDirectories("_*"))
            {
                yield return new object[] {directoryInfo.FullName};
            }
        }
    }
}