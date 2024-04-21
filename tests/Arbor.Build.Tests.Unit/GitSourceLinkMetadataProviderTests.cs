using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Arbor.FS;
using FluentAssertions;
using Serilog.Core;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class GitSourceLinkMetadataProviderTests
{
    [Fact]
    public async Task ShouldCreate()
    {
        using IFileSystem fs = new MemoryFileSystem();
        var provider = new GitSourceLinkMetadataProvider(fs);

        var buildDirectory = @"c:\build".ParseAsPath();

        fs.CreateDirectory(buildDirectory);

        var buildVariables =
            new List<IVariable>
            {
                new BuildVariable(WellKnownVariables.DeterministicBuildEnabled, "true"),
                new BuildVariable(WellKnownVariables.GitHash, "AAAA"),
                new BuildVariable(WellKnownVariables.RepositoryUrl, "http://build.local"),
                new BuildVariable(WellKnownVariables.SourceRoot, buildDirectory.FullName)
            };

        string[] args = [];

        var exitCode = await provider.ExecuteAsync(Logger.None, buildVariables, args, CancellationToken.None);

        exitCode.IsSuccess.Should().BeTrue();

        fs.FileExists(@"c:\build\.git\HEAD".ParseAsPath()).Should().BeTrue();
        fs.FileExists(@"c:\build\.git\config".ParseAsPath()).Should().BeTrue();
    }
}