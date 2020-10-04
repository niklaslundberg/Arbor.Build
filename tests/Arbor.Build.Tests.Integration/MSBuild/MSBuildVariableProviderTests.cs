using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.MSBuild
{
    public class MSBuildVariableProviderTests
    {
        public MSBuildVariableProviderTests(ITestOutputHelper output) => this._output = output;

        readonly ITestOutputHelper _output;

        [Fact(Skip = "Requires VS 2019 installed")]
        public async Task GetMSBuildVariables()
        {
            using var physicalFileSystem = new PhysicalFileSystem();
            var msBuildVariableProvider = new MSBuildVariableProvider(EnvironmentVariables.Empty, SpecialFolders.Default, physicalFileSystem);
            var variables = await msBuildVariableProvider.GetBuildVariablesAsync(Logger.None,
                ImmutableArray<IVariable>.Empty, CancellationToken.None).ConfigureAwait(false);

            _output.WriteLine(string.Join(Environment.NewLine, variables.Select(s => s.Key + " " + s.Value)));

        }
    }
}
