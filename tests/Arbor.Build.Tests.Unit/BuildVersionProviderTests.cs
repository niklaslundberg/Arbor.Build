using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Time;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.Versioning;
using Arbor.FS;
using Serilog.Core;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class BuildVersionProviderTests
{
    [Fact]
    public async Task Should()
    {
        ITimeService testTimeService = new TestTimeService(new DateTime(637134888390000000));
        using var memoryFileSystem = new MemoryFileSystem();
        var buildVersionProvider = new BuildVersionProvider(testTimeService, new BuildContext(memoryFileSystem)
        {
            SourceRoot = new DirectoryEntry(memoryFileSystem, "/temp/build").EnsureExists()
        });

        var buildVariables =
            new List<IVariable>
            {
                new BuildVariable(WellKnownVariables.SourceRoot, Path.GetTempPath().ParseAsPath().FullName),
                new BuildVariable(WellKnownVariables.BuildNumberAsUnixEpochSecondsEnabled, "true")
            };

        var variables = await buildVersionProvider.GetBuildVariablesAsync(Logger.None, buildVariables,
            CancellationToken.None);

        string? fullVersion = variables.GetVariableValueOrDefault(WellKnownVariables.Version, "");

        Assert.Equal("0.1.0.1577892039", fullVersion);
        Assert.Equal("1577892039", variables.GetVariableValueOrDefault(WellKnownVariables.VersionBuild, ""));
    }
}