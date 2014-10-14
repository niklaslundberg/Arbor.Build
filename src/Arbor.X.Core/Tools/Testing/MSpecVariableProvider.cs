﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Testing
{
    public class MSpecVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue().Value;

            var reportDirectory = new DirectoryInfo(reportPath);

            var testReportPathDirectory = new DirectoryInfo(Path.Combine(reportDirectory.FullName, "Machine.Specifications"));

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