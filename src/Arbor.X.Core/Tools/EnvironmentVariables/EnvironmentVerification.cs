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

            if (missingKeys.Any())
            {
                sb.AppendLine($"Missing variables: [{missingKeys.Count}]");

                foreach (string missingKey in missingKeys)
                {
                    sb.AppendLine(missingKey);
                }
            }

            if (missingValues.Any())
            {
                sb.AppendLine($"Variables with empty values: [{missingValues.Count}]");
                foreach (string missingValue in missingValues)
                {
                    sb.AppendLine(missingValue);
                }
            }

            bool succeeded = !missingKeys.Any() && !missingValues.Any();

            succeeded &= await PostVariableVerificationAsync(sb, buildVariables, logger);

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
