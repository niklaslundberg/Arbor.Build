using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Testing
{
    public class VSTestVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger,
                                                                         IReadOnlyCollection<IVariable> buildVariables)
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
    }
}