namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        public static class TeamCity
        {
            [VariableDescription("TeamCity Build Version")]
            public const string TeamCityVersionBuild =
                "BUILD_NUMBER";

            [VariableDescription("Flag to indiciate if running in TeamCity (calculated)")]
            public const string ExternalTools_TeamCity_IsRunningInTeamCity =
                "Arbor.X.Tools.External.TeamCity.IsRunningInTeamCity";

            [VariableDescription("TeamCity Version")]
            public const string ExternalTools_TeamCity_TeamCityVersion =
                "TEAMCITY_VERSION";

            [VariableDescription("TeamCity Build Version")]
            public const string TeamCityVcsNumber =
                "BUILD_VCS_NUMBER";

            [VariableDescription("TeamCity build configuration name")]
            public const string ExternalTools_TeamCity_BuildConfigurationName = "TEAMCITY_BUILDCONF_NAME";
        }
    }
}
