namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Major version")]
        public static readonly string VersionMajor = "Version.Major";

        [VariableDescription("Minor version")]
        public static readonly string VersionMinor = "Version.Minor";

        [VariableDescription("Patch version")]
        public static readonly string VersionPatch = "Version.Patch";

        [VariableDescription("Build version")]
        public static readonly string VersionBuild = "Version.Build";
    }
}
