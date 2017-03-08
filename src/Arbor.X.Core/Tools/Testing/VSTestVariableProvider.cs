using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Testing
{
    [UsedImplicitly]
    public class VsTestVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            string reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue().Value;

            DirectoryInfo reportDirectory = new DirectoryInfo(reportPath);

            DirectoryInfo vsTestReportPathDirectory = new DirectoryInfo(Path.Combine(reportDirectory.FullName, "VSTest"));

            vsTestReportPathDirectory.EnsureExists();

            EnvironmentVariable[] environmentVariables = {
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
