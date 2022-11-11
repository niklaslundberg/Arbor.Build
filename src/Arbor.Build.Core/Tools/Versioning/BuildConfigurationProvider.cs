using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.MSBuild;
using JetBrains.Annotations;
using NuGet.Packaging;
using Serilog;

namespace Arbor.Build.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class BuildConfigurationProvider : IVariableProvider
    {
        public const int ProviderOrder = 10;
        private readonly BuildContext _buildContext;

        public BuildConfigurationProvider(BuildContext buildContext) => _buildContext = buildContext;

        public int Order => ProviderOrder;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (buildVariables.GetVariableValueOrDefault(WellKnownVariables.CurrentBuildConfiguration) is null)
            {
                variables.Add(new FunctionVariable(
                    WellKnownVariables.CurrentBuildConfiguration,
                    () => _buildContext.CurrentBuildConfiguration?.Configuration));
            }

            bool? releaseEnabled = buildVariables.GetOptionalBooleanByKey(WellKnownVariables.ReleaseBuildEnabled);

            bool? debugEnabled =
                buildVariables.GetOptionalBooleanByKey(WellKnownVariables.DebugBuildEnabled);

            _buildContext.Configurations.AddRange(buildVariables.GetVariableValueOrDefault(WellKnownVariables.Configurations, "")!
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            if (_buildContext.Configurations.Count == 0)
            {
                if (releaseEnabled == true)
                {
                    _buildContext.Configurations.Add(WellKnownConfigurations.Release);
                }

                if (debugEnabled == true)
                {
                    _buildContext.Configurations.Add(WellKnownConfigurations.Debug);
                }
            }

            if (_buildContext.Configurations.Count == 0)
            {
                string configuration = GetConfiguration(buildVariables);
                _buildContext.Configurations.Add(configuration);
            }

            return Task.FromResult(variables.ToImmutableArray());
        }

        private static string GetConfiguration(IReadOnlyCollection<IVariable> buildVariables)
        {
            string? branchName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                return WellKnownConfigurations.Debug;
            }

            var branch = new BranchName(branchName);

            bool isReleaseBranch = branch.IsProductionBranch();

            if (isReleaseBranch)
            {
                return WellKnownConfigurations.Release;
            }

            if (branch.IsFeatureBranch())
            {
                string? featureBranchConfiguration = buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.FeatureBranchDefaultConfiguration);

                if (!string.IsNullOrWhiteSpace(
                    featureBranchConfiguration))
                {
                    return featureBranchConfiguration;
                }
            }

            return WellKnownConfigurations.Debug;
        }
    }
}
