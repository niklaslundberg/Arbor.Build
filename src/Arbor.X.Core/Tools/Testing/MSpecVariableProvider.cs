using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;

using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.Testing
{
    [UsedImplicitly]
    public class MSpecVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue().Value;

            var reportDirectory = new DirectoryInfo(reportPath);

            var testReportPathDirectory = new DirectoryInfo(Path.Combine(
                reportDirectory.FullName,
                MachineSpecificationsConstants.MachineSpecificationsName));

            testReportPathDirectory.EnsureExists();

            var environmentVariables = new[]
            {
                new EnvironmentVariable(
                    WellKnownVariables.ExternalTools_MSpec_ReportPath,
                    testReportPathDirectory.FullName)
            };

            return Task.FromResult<
                IEnumerable<IVariable>>(environmentVariables);
        }
    }
}
