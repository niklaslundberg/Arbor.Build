using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Tests.Integration.Bootstrapper;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.MSBuild
{
    public class MSBuildVariableProviderTests
    {
        public MSBuildVariableProviderTests(ITestOutputHelper output) => this.output = output;

        readonly ITestOutputHelper output;

        [Fact(Skip = "Requires VS 2019 installed")]
        public async Task GetMSbuildVariables()
        {
            var msBuildVariableProvider = new MSBuildVariableProvider(EnvironmentVariables.Empty, SpecialFolders.Default, new PhysicalFileSystem());
            var variables = await msBuildVariableProvider.GetBuildVariablesAsync(Logger.None,
                ImmutableArray<IVariable>.Empty, CancellationToken.None).ConfigureAwait(false);

            output.WriteLine(string.Join(Environment.NewLine, variables.Select(s => s.Key + " " + s.Value)));

        }
    }
}
