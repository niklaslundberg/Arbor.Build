using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Configuration.AutofacModules;
using Arbor.X.Core.Tools.Git;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class BuildConfigurationProvider : IVariableProvider
    {
        public const int ProviderOrder = 10;
        private readonly BuildContext _buildContext;

        public BuildConfigurationProvider(BuildContext buildContext)
        {
            _buildContext = buildContext;
        }

        public int Order => ProviderOrder;

        public Task<IEnumerable<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyConfiguration, null) == null)
            {
                variables.Add(new FunctionVariable(
                    WellKnownVariables.NetAssemblyConfiguration,
                    () => _buildContext.CurrentBuildConfiguration?.Configuration));
            }

            bool releaseEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.ReleaseBuildEnabled, true);

            bool debugEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DebugBuildEnabled, true);

            if (!debugEnabled && releaseEnabled)
            {
                variables.Add(new BuildVariable(WellKnownVariables.Configuration, "release"));
            }

            if (debugEnabled && !releaseEnabled)
            {
                variables.Add(new BuildVariable(WellKnownVariables.Configuration, "debug"));
            }

            string branchName = buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName, "");

            bool isReleaseBuild = IsReleaseBuild(branchName);

            variables.Add(new BuildVariable(WellKnownVariables.ReleaseBuild,
                isReleaseBuild.ToString().ToLowerInvariant()));

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        private static string GetConfiguration([NotNull] string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            bool isReleaseBranch = new BranchName(branchName).IsProductionBranch();

            if (isReleaseBranch)
            {
                return "release";
            }

            return "debug";
        }

        private static bool IsReleaseBuild(string branchName)
        {
            bool isProductionBranch = new BranchName(branchName).IsProductionBranch();

            return isProductionBranch;
        }
    }
}
