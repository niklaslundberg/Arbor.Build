using System;
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

namespace Arbor.Build.Tests.Unit
{
    public class GitSourceLinkMetadataProviderTests
    {
        [Fact]
        public async Task ShouldCreate()
        {
            IFileSystem fs = new MemoryFileSystem();
            var provider = new GitSourceLinkMetadataProvider(fs);

            var winFS = new WindowsFs(new PhysicalFileSystem());

            UPath buildDirectory = winFS.ConvertPathFromInternal(@"c:\build");

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

            fs.FileExists(winFS.ConvertPathFromInternal(@"c:\build\.git\HEAD")).Should().BeTrue();
            fs.FileExists(winFS.ConvertPathFromInternal(@"c:\build\.git\config")).Should().BeTrue();
        }
    }
}