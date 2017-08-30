using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Kudu
{
    public static class KuduHelper
    {
        public static bool IsKuduAware(IReadOnlyCollection<IVariable> buildVariables, ILogger loggerOption = null)
        {
            ILogger logger = loggerOption ??
                             new DelegateLogger(
                                 (info, prefix) => { },
                                 (warning, prefix) => { },
                                 (error, prefix) => { });

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
                            logger.WriteVerbose(
                                $"Build variable {WellKnownVariables.ExternalTools_Kudu_Platform} is missing");
                        }
                    }
                    else
                    {
                        logger.WriteVerbose(
                            $"Build variable {WellKnownVariables.ExternalTools_Kudu_DeploymentTarget} is missing");
                    }
                }
                else
                {
                    logger.WriteVerbose($"No build variables starts with {Pattern}");
                }
            }
            else
            {
                logger.WriteVerbose(
                    $"Build varaible {WellKnownVariables.ExternalTools_Kudu_Enabled} is set to {buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value}");
            }

            return isKuduAware;
        }
    }
}
