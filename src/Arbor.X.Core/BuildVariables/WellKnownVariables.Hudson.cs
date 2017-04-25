namespace Arbor.X.Core.BuildVariables
{
    public partial class WellKnownVariables
    {
        [VariableDescription("Hudson HOME path")]
        public static readonly string ExternalTools_Hudson_HudsonHome =
            "HUDSON_HOME";

        [VariableDescription("Flag to indiciate if running in Hudson (calculated)")]
        public static readonly string ExternalTools_Hudson_IsRunningInHudson =
            "Arbor.X.Tools.External.Hudson.IsRunningInHudson";
    }
}
