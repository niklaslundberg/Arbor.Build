using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.EnvironmentVariables
{
    public abstract class EnvironmentVerification : ITool
    {
        protected readonly List<string> RequiredValues = new List<string>();

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            logger ??= Logger.None;

            List<string> missingKeys =
                RequiredValues.Where(
                        var =>
                            !buildVariables.Any(
                                required => required.Key.Equals(var, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            List<string> missingValues =
                RequiredValues.Where(
                        var =>
                        {
                            IVariable value =
                                buildVariables.SingleOrDefault(
                                    required => required.Key.Equals(var, StringComparison.OrdinalIgnoreCase));

                            return value != null && string.IsNullOrWhiteSpace(value.Value);
                        })
                    .ToList();

            var sb = new StringBuilder();

            if (missingKeys.Count > 0)
            {
                sb.Append("Missing variables: [").Append(missingKeys.Count).AppendLine("]");

                foreach (string missingKey in missingKeys)
                {
                    sb.AppendLine(missingKey);
                }
            }

            if (missingValues.Count > 0)
            {
                sb.Append("Variables with empty values: [").Append(missingValues.Count).AppendLine("]");
                foreach (string missingValue in missingValues)
                {
                    sb.AppendLine(missingValue);
                }
            }

            bool succeeded = missingKeys.Count == 0 && missingValues.Count == 0;

            succeeded &= await PostVariableVerificationAsync(sb, buildVariables, logger).ConfigureAwait(false);

            if (!succeeded)
            {
                logger.Error("{Message}", sb.ToString());
            }

            return succeeded ? ExitCode.Success : ExitCode.Failure;
        }

        protected virtual Task<bool> PostVariableVerificationAsync(
            StringBuilder variableBuilder,
            IReadOnlyCollection<IVariable> buildVariables,
            ILogger logger) => Task.FromResult(true);
    }
}
