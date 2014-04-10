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
            var logger = loggerOption ?? new DelegateLogger(i => { }, w => { }, e => { });

            bool isKuduAware = false;

            if (!buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled))
            {
                const string pattern = "kudu_";

                if (buildVariables.Any(bv => bv.Key.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_DeploymentTarget))
                    {
                        if (buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Platform))
                        {
                            isKuduAware = true;
                        }
                        else
                        {
                            logger.WriteVerbose(string.Format("Build variable {0} is missing", WellKnownVariables.ExternalTools_Kudu_Platform));
                        }
                    }
                    else
                    {
                        logger.WriteVerbose(string.Format("Build variable {0} is missing", WellKnownVariables.ExternalTools_Kudu_DeploymentTarget));
                    }
                }
                else
                {
                    logger.WriteVerbose(string.Format("No build variables starts with {0}", pattern));
                }
            }
            else
            {
                logger.WriteVerbose(string.Format("Build varaible {0} is set to {1}", WellKnownVariables.ExternalTools_Kudu_Enabled, buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value));
            }

            return isKuduAware;
        }
    }
}