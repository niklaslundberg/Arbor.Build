using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.Testing
{
    public class VsTestVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue().Value;

            var reportDirectory = new DirectoryInfo(reportPath);

            var vsTestReportPathDirectory = new DirectoryInfo(Path.Combine(reportDirectory.FullName, "VSTest"));

            vsTestReportPathDirectory.EnsureExists();

            var environmentVariables = new[]
                                           {
                                               new EnvironmentVariable(
                                                   WellKnownVariables.ExternalTools_VSTest_ReportPath,
                                                   vsTestReportPathDirectory.FullName)
                                           };

            return Task.FromResult<
                IEnumerable<IVariable>>(environmentVariables);
        }
        public int Order => VariableProviderOrder.Ignored;
    }
}