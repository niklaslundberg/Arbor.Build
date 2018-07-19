using System; using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;

namespace Arbor.X.Core.Tools.EnvironmentVariables
{
    public abstract class EnvironmentVerification : ITool
    {
        protected readonly List<string> RequiredValues = new List<string>();

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            List<string> missingKeys =
                RequiredValues.Where(
                        var =>
                            !buildVariables.Any(
                                required => required.Key.Equals(var, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

            List<string> missingValues =
                RequiredValues.Where(
                        var =>
                        {
                            IVariable value =
                                buildVariables.SingleOrDefault(
                                    required => required.Key.Equals(var, StringComparison.InvariantCultureIgnoreCase));

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
                logger.Error(sb.ToString());
            }

            return succeeded ? ExitCode.Success : ExitCode.Failure;
        }

        protected virtual Task<bool> PostVariableVerificationAsync(
            StringBuilder variableBuilder,
            IReadOnlyCollection<IVariable> buildVariables,
            ILogger logger)
        {
            return Task.FromResult(true);
        }
    }
}
