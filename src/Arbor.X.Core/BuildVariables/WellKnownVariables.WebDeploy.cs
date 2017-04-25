namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
        public static readonly string WebDeployBuildPackages = Arbor.X.Build + ".WebDeploy.BuildPackagesEnabled";

        [VariableDescription("Flag to indicate if Web Deploy pre compilation should be enabled")]
        public static readonly string WebDeployPreCompilationEnabled =
            Arbor.X.Build + ".WebDeploy.PreCompilation.Enabled";
    }
}
