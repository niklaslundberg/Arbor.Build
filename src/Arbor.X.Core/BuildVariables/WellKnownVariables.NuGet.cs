namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if NuGet package creation is enabled")]
        public static readonly string NuGetPackageEnabled = Arbor.X + ".NuGet.Package.Enabled";

        [VariableDescription("NuGet package artifacts suffix")]
        public static readonly string NuGetPackageArtifactsSuffix = Arbor.X + ".NuGet.Package.Artifacts.Suffix";

        [VariableDescription("Flag to indicate if the build number is included in the NuGet package artifacts")]
        public static readonly string BuildNumberInNuGetPackageArtifactsEnabled = Arbor.X + ".NuGet.Package.Artifacts.BuildNumber.Enabled";

        [VariableDescription("NuGet executable path (eg. C:\\nuget.exe)")]
        public static readonly string ExternalTools_NuGet_ExePath = "Arbor.X.Tools.External.NuGet.ExePath";

        [VariableDescription("Flag to indicate if created NuGet (binary+symbol) packages should be kept in the same folder", "true")]
        public static readonly string NuGetKeepBinaryAndSymbolPackagesTogetherEnabled = "Arbor.X.NuGet.Package.Artifacts.KeepBinaryAndSymbolTogetherEnabled";
        
        [VariableDescription("Flag to indicate if NuGet packages should be created regardless of the branch convention", "false")]
        public static readonly string NuGetCreatePackagesOnAnyBranchEnabled = "Arbor.X.NuGet.Package.Artifacts.CreateOnAnyBranchEnabled";
    }
}
