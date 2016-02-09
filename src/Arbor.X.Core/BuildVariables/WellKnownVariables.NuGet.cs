namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if NuGet package creation is enabled")]
        public static readonly string NuGetPackageEnabled = Arbor.X + ".NuGet.Package.Enabled";

        [VariableDescription("Specific URI to download nuget.exe from")]
        public static readonly string NuGetExeDownloadUri = Arbor.X + ".NuGet.DownloadUri";

        [VariableDescription("Flag to indicate if NuGet package creation is enabled")]
        public static readonly string NuGetPackageExcludesCommaSeparated = Arbor.X + ".NuGet.Package.ExcludesCommaSeparated";

        [VariableDescription("NuGet package artifacts suffix")]
        public static readonly string NuGetPackageArtifactsSuffix = Arbor.X + ".NuGet.Package.Artifacts.Suffix";

        [VariableDescription("NuGet package id override")]
        public static readonly string NuGetPackageIdOverride = Arbor.X + ".NuGet.Package.Artifacts.PackageId.Override";

        [VariableDescription("NuGet package version override")]
        public static readonly string NuGetPackageVersionOverride = Arbor.X + ".NuGet.Package.Artifacts.Version.Override";

        [VariableDescription("Allow NuGet package manifest rewrite")]
        public static readonly string NuGetAllowManifestReWrite = Arbor.X + ".NuGet.Package.AllowManifestReWriteEnabled";

        [VariableDescription("Flag to indicate if creation of NuGet source packages is enabled")]
        public static readonly string NuGetSymbolPackagesEnabled = Arbor.X + ".NuGet.Package.Symbols.Enabled";

        [VariableDescription("Flag to indicate if creation of NuGet web packages is enabled")]
        public static readonly string NugetCreateNuGetWebPackagesEnabled = Arbor.X + ".NuGet.Package.CreateNuGetWebPackages.Enabled";

        [VariableDescription("Flag to indicate if creation of NuGet web package is enabled for a project")]
        public static readonly string NugetCreateNuGetWebPackageForProjectEnabledFormat = "{0}_Arbor_X_NuGet_Package_CreateNuGetWebPackageForProject_Enabled";

        [VariableDescription("Flag to indicate if the build number is included in the NuGet package artifacts")]
        public static readonly string BuildNumberInNuGetPackageArtifactsEnabled = Arbor.X + ".NuGet.Package.Artifacts.BuildNumber.Enabled";

        [VariableDescription("Flag to indicate if the NuGet package id has branch name", "false")]
        public static readonly string NuGetPackageIdBranchNameEnabled = Arbor.X + ".NuGet.Package.Artifacts.PackageId.BranchNameEnabled";

        [VariableDescription("NuGet executable path (eg. C:\\nuget.exe)")]
        public static readonly string ExternalTools_NuGet_ExePath = "Arbor.X.Tools.External.NuGet.ExePath";

        [VariableDescription("NuGet executable path (eg. C:\\nuget.exe)")]
        public static readonly string ExternalTools_NuGet_ExePath_Custom = "Arbor.X.Tools.External.NuGet.ExePath.Custom";

        [VariableDescription("Flag to indicate if created NuGet (binary+symbol) packages should be kept in the same folder", "true")]
        public static readonly string NuGetKeepBinaryAndSymbolPackagesTogetherEnabled = "Arbor.X.NuGet.Package.Artifacts.KeepBinaryAndSymbolTogetherEnabled";

        [VariableDescription("Flag to indicate if NuGet packages should be created regardless of the branch convention", "false")]
        public static readonly string NuGetCreatePackagesOnAnyBranchEnabled = "Arbor.X.NuGet.Package.Artifacts.CreateOnAnyBranchEnabled";

        [VariableDescription("Flag to indicate if NuGet should try to update itself during start", "false")]
        public static readonly string NuGetVersionUpdatedEnabled = "Arbor.X.NuGet.VersionUpdateEnabled";

        [VariableDescription("Flag to indicate if NuGet packages should be created regardless of the branch convention", "false")]
        public static readonly string NuGetReinstallArborPackageEnabled = "Arbor.X.NuGet.ReinstallArborPackageEnabled";

        [VariableDescription("Flag to indicate if NuGet self update is enabled", "true")]
        public static readonly string NuGetSelfUpdateEnabled = "Arbor.X.NuGet.SelfUpdate.Enabled";

        public const string NuGetRestoreDisableParallelProcessing = "Arbor.X.NuGet.Restore.DisableParallelProcessing";

        public const string NuGetRestoreNoCache = "Arbor.X.NuGet.Restore.NoCache";

        public const string ExternalTools_NuGetServer_Enabled = "Arbor.X.NuGet.PackageUpload.Enabled";

        public const string ExternalTools_NuGetServer_WebSitePackagesUploadEnabled = "Arbor.X.NuGet.WebsitePackages.PackageUpload.Enabled";

        public const string ExternalTools_NuGetServer_Uri = "Arbor.X.NuGet.PackageUpload.Server.Uri";

        public const string ExternalTools_NuGetServer_ApiKey = "Arbor.X.NuGet.PackageUpload.Server.ApiKey";

        public const string ExternalTools_NuGetServer_ForceUploadEnabled = "Arbor.X.NuGet.PackageUpload.ForceUploadEnabled";

        public const string ExternalTools_NuGetServer_UploadTimeoutInSeconds = "Arbor.X.NuGet.PackageUpload.TimeoutInSeconds";
    }
}
