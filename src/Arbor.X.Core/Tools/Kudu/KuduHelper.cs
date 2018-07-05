using System; using Serilog;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.BuildVariables;


namespace Arbor.X.Core.Tools.Kudu
{
    public static class KuduHelper
    {
        public static bool IsKuduAware(IReadOnlyCollection<IVariable> buildVariables, ILogger logger = null)
        {
            bool isKuduAware = false;

            if (!buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled))
            {
                const string Pattern = "kudu_";

                if (buildVariables.Any(bv => bv.Key.StartsWith(Pattern, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_DeploymentTarget))
                    {
                        if (buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Platform))
                        {
                            isKuduAware = true;
                        }
                        else
                        {
                            logger?.Verbose("Build variable {ExternalTools_Kudu_Platform} is missing", WellKnownVariables.ExternalTools_Kudu_Platform);
                        }
                    }
                    else
                    {
                        logger?.Verbose("Build variable {ExternalTools_Kudu_DeploymentTarget} is missing", WellKnownVariables.ExternalTools_Kudu_DeploymentTarget);
                    }
                }
                else
                {
                    logger?.Verbose("No build variables starts with {Pattern}", Pattern);
                }
            }
            else
            {
                logger?.Verbose("Build varaible {ExternalTools_Kudu_Enabled} is set to {Value}", WellKnownVariables.ExternalTools_Kudu_Enabled, buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value);
            }

            return isKuduAware;
        }
    }
}
