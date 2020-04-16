using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.Versioning;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.Build.Tests.Integration.MSBuild
{
    public class BuildConfigurationProviderTests
    {
        public BuildConfigurationProviderTests(ITestOutputHelper testOutputHelper) =>
            _testOutputHelper = testOutputHelper;

        readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public async Task DefaultConfigurationShouldBeDebug()
        {
            var buildContext = new BuildContext();

            var buildConfigurationProvider = new BuildConfigurationProvider(buildContext);

            var variables = await buildConfigurationProvider.GetBuildVariablesAsync(Logger.None, new List<IVariable>(),
                CancellationToken.None);

            foreach (var variable in variables)
            {
                _testOutputHelper.WriteLine($"{variable.Key}: {variable.Value}");
            }
            Assert.Contains(WellKnownConfigurations.Debug, buildContext.Configurations);
        }

        [InlineData("feature-abc123")]
        [InlineData("debug")]
        [InlineData("unknown")]
        [InlineData("any")]
        [Theory]
        public async Task BranchConfigurationShouldBeDebug(string branchName)
        {
            var buildContext = new BuildContext();

            var buildConfigurationProvider = new BuildConfigurationProvider(buildContext);

            var variables = await buildConfigurationProvider.GetBuildVariablesAsync(Logger.None, new List<IVariable>()
                {
                    new BuildVariable(WellKnownVariables.BranchName, branchName)
                },
                CancellationToken.None);

            foreach (var variable in variables)
            {
                _testOutputHelper.WriteLine($"{variable.Key}: {variable.Value}");
            }

            Assert.Contains(WellKnownConfigurations.Debug, buildContext.Configurations);
        }

        [InlineData("feature-abc123")]
        [InlineData("unknown")]
        [InlineData("any")]
        [Theory]
        public async Task BranchConfigurationShouldBeDefaultForFeatureBranch(string branchName)
        {
            var buildContext = new BuildContext();

            var buildConfigurationProvider = new BuildConfigurationProvider(buildContext);

            var variables = await buildConfigurationProvider.GetBuildVariablesAsync(Logger.None, new List<IVariable>()
                {
                    new BuildVariable(WellKnownVariables.BranchName, branchName),
                    new BuildVariable(WellKnownVariables.FeatureBranchDefaultConfiguration, "customDefault")
                },
                CancellationToken.None);

            foreach (var variable in variables)
            {
                _testOutputHelper.WriteLine($"{variable.Key}: {variable.Value}");
            }

            Assert.Contains("customDefault", buildContext.Configurations);
        }

        [InlineData("master")]
        [InlineData("release-v123")]
        [InlineData("release")]
        [InlineData("release/123")]
        [Theory]
        public async Task BranchConfigurationShouldBeRelease(string branchName)
        {
            var buildContext = new BuildContext();

            var buildConfigurationProvider = new BuildConfigurationProvider(buildContext);

            var variables = await buildConfigurationProvider.GetBuildVariablesAsync(Logger.None, new List<IVariable>()
                {
                    new BuildVariable(WellKnownVariables.BranchName, branchName)
                },
                CancellationToken.None);

            foreach (var variable in variables)
            {
                _testOutputHelper.WriteLine($"{variable.Key}: {variable.Value}");
            }

            Assert.Contains(WellKnownConfigurations.Release, buildContext.Configurations);
        }
    }
}