using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescriptionAttribute("Flag to indiciate if running in TeamCity (calculated)")]
        public static readonly string ExternalTools_TeamCity_IsRunningInTeamCity = "Arbor.X.Tools.External.TeamCity.IsRunningInTeamCity";

        [VariableDescriptionAttribute("TeamCity build configuration name")]
        public static readonly string ExternalTools_TeamCity_BuildConfigurationName = "TEAMCITY_BUILDCONF_NAME";
    }
}
