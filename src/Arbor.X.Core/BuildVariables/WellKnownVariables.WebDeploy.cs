namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
        public static readonly string WebDeployBuildPackages = Arbor.X.Build + ".WebDeploy.BuildPackagesEnabled";
    }
}