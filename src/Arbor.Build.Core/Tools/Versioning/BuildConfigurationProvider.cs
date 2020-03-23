using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.MSBuild;
using JetBrains.Annotations;
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

            if (buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyConfiguration, null) is null)
            {
                variables.Add(new FunctionVariable(
                    WellKnownVariables.NetAssemblyConfiguration,
                    () => _buildContext.CurrentBuildConfiguration?.Configuration));
            }

            bool releaseEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.ReleaseBuildEnabled, true);

            bool debugEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DebugBuildEnabled, true);

            if (!buildVariables.HasKey(WellKnownVariables.Configuration))
            {
                if (!debugEnabled && releaseEnabled)
                {
                    variables.Add(new BuildVariable(WellKnownVariables.Configuration, WellKnownConfigurations.Release));
                }
                else if (debugEnabled && !releaseEnabled)
                {
                    variables.Add(new BuildVariable(WellKnownVariables.Configuration, WellKnownConfigurations.Debug));
                }
                else
                {
                    string configuration = GetConfiguration(buildVariables);
                    variables.Add(new BuildVariable(WellKnownVariables.Configuration, configuration));
                }
            }

            string branchName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName, "");

            bool isReleaseBuild = IsReleaseBuild(branchName);

            variables.Add(new BuildVariable(WellKnownVariables.ReleaseBuild,
                isReleaseBuild.ToString().ToLowerInvariant()));

            return Task.FromResult(variables.ToImmutableArray());
        }

        private static string GetConfiguration(IReadOnlyCollection<IVariable> buildVariables)
        {
            string? branchName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName, null);

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
                string? featureBranchConfiguration = buildVariables.GetVariableValueOrDefault(WellKnownVariables.FeatureBranchDefaultConfiguration,
                    null);

                if (!string.IsNullOrWhiteSpace(
                    featureBranchConfiguration))
                {
                    return featureBranchConfiguration;
                }
            }

            return WellKnownConfigurations.Debug;
        }

        private static bool IsReleaseBuild(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return false;
            }

            bool isProductionBranch = new BranchName(branchName).IsProductionBranch();

            return isProductionBranch;
        }
    }
}
