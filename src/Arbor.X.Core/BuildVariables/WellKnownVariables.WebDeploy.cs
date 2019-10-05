namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
        public const string WebDeployBuildPackages = "Arbor.Build.Build.WebDeploy.BuildPackagesEnabled";

        [VariableDescription("Flag to indicate if Web Deploy pre compilation should be enabled")]
        public const string WebDeployPreCompilationEnabled =
            "Arbor.Build.Build.WebDeploy.PreCompilation.Enabled";
    }
}
