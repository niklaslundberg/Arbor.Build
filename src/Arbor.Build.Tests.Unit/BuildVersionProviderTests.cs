using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Versioning;
using Serilog.Core;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class BuildVersionProviderTests
    {
        [Fact]
        public async Task Should()
        {
            ITimeService testTimeService = new TestTimeService(new DateTime(637134888390000000));
            var buildVersionProvider = new BuildVersionProvider(testTimeService);

            var buildVariables =
                new List<IVariable>
                {
                    new BuildVariable(WellKnownVariables.SourceRoot, Path.GetTempPath()),
                    new BuildVariable(WellKnownVariables.BuildNumberAsUnixEpochSecondsEnabled, "true")
                };

            var variables = await buildVersionProvider.GetBuildVariablesAsync(Logger.None, buildVariables,
                CancellationToken.None);

            string? fullVersion = variables.GetVariableValueOrDefault(WellKnownVariables.Version, "");

            Assert.Equal("0.1.0.1577892039", fullVersion);
            Assert.Equal("1577892039", variables.GetVariableValueOrDefault(WellKnownVariables.VersionBuild, ""));
        }
    }
}