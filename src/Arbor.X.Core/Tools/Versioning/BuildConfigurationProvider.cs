using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class BuildConfigurationProvider : IVariableProvider
    {
        public const int ProviderOrder = 10;

        public int Order => ProviderOrder;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyConfiguration, null) == null)
            {
                variables.Add(new DynamicVariable(
                    WellKnownVariables.NetAssemblyConfiguration,
                    () =>
                    {
                        string currentBuildConfiguration =
                            Environment.GetEnvironmentVariable(WellKnownVariables.CurrentBuildConfiguration);

                        return currentBuildConfiguration;
                    }));
            }

            bool releaseEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.ReleaseBuildEnabled, defaultValue: true);

            bool debugEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DebugBuildEnabled, defaultValue: true);

            if (!debugEnabled && releaseEnabled)
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.Configuration, "release"));
            }

            if (debugEnabled && !releaseEnabled)
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.Configuration, "debug"));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
