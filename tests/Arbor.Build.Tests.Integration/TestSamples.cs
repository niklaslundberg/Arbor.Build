using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Logger = Serilog.Core.Logger;

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
            var environmentVariables = new FallbackEnvironment(new EnvironmentVariables(), new DefaultEnvironmentVariables());

            environmentVariables.SetEnvironmentVariable(WellKnownVariables.BranchName, "develop");
            environmentVariables.SetEnvironmentVariable(WellKnownVariables.SourceRoot, path);

            var logger = _testOutputHelper.CreateTestLogger();
            BuildApplication buildApplication = new BuildApplication(logger, environmentVariables, SpecialFolders.Default);

            var args = Array.Empty<string>();

           var exitCode =  await buildApplication.RunAsync(args);

           Assert.Equal(0, exitCode);

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

    public class FallbackEnvironment : IEnvironmentVariables
    {
        readonly IEnvironmentVariables _environmentVariables;
        readonly IEnvironmentVariables _defaultEnvironmentVariables;

        public FallbackEnvironment(IEnvironmentVariables environmentVariables, IEnvironmentVariables defaultEnvironmentVariables)
        {
            _environmentVariables = environmentVariables;
            _defaultEnvironmentVariables = defaultEnvironmentVariables;
        }

        public string? GetEnvironmentVariable(string key) => _environmentVariables.GetEnvironmentVariable(key) ?? _defaultEnvironmentVariables.GetEnvironmentVariable(key);

        public ImmutableArray<KeyValuePair<string, string?>> GetVariables() => _environmentVariables.GetVariables().Concat(_defaultEnvironmentVariables.GetVariables()).ToImmutableArray();

        public void SetEnvironmentVariable(string key, string? value) => _environmentVariables.SetEnvironmentVariable(key, value);
    }
}