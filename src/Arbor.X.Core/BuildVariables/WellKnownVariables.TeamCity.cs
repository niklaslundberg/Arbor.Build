namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indiciate if running in TeamCity (calculated)")]
        public static readonly string ExternalTools_TeamCity_IsRunningInTeamCity = "Arbor.X.Tools.External.TeamCity.IsRunningInTeamCity";

        [VariableDescription("TeamCity Version")]
        public static readonly string ExternalTools_TeamCity_TeamCityVersion = "TEAMCITY_VERSION";

        [VariableDescription("TeamCity build configuration name")]
        public static readonly string ExternalTools_TeamCity_BuildConfigurationName = "TEAMCITY_BUILDCONF_NAME";
    }
}
