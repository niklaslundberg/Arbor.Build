using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Git;
using FluentAssertions;
using Serilog.Core;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit
{
    public class GitSourceLinkMetadataProviderTests
    {
        [Fact]
        public async Task ShouldCreate()
        {
            using IFileSystem fs = new MemoryFileSystem();
            var provider = new GitSourceLinkMetadataProvider(fs);

            var buildDirectory = @"c:\build".AsFullPath();

            fs.CreateDirectory(buildDirectory);

            var buildVariables =
                new List<IVariable>
                {
                    new BuildVariable(WellKnownVariables.DeterministicBuildEnabled, "true"),
                    new BuildVariable(WellKnownVariables.GitHash, "AAAA"),
                    new BuildVariable(WellKnownVariables.RepositoryUrl, "http://build.local"),
                    new BuildVariable(WellKnownVariables.SourceRoot, buildDirectory.FullName)
                };

            var args = Array.Empty<string>();

            var exitCode = await provider.ExecuteAsync(Logger.None, buildVariables, args, CancellationToken.None);

            exitCode.IsSuccess.Should().BeTrue();

            fs.FileExists(@"c:\build\.git\HEAD".AsFullPath()).Should().BeTrue();
            fs.FileExists(@"c:\build\.git\config".AsFullPath()).Should().BeTrue();
        }
    }
}