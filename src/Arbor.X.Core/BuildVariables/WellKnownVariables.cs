using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Arbor.X.Core.GenericExtensions;

namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
// ReSharper disable InconsistentNaming

// ReSharper disable ConvertToConstant.Global

        [VariableDescription("Visual Studio version")]
        public static readonly string ExternalTools_VisualStudio_Version =
            "Arbor.X.Tools.External.VisualStudio.Version";

        [VariableDescription("Visual Studio version")]
        public static readonly string ExternalTools_VisualStudio_Version_Allow_PreRelease =
            "Arbor.X.Tools.External.VisualStudio.Version.PreRelease.Enabled";

        [VariableDescription("Build arftifacts path")]
        public static readonly string Artifacts = "Arbor.X.Artifacts";

        [VariableDescription("Flag to indicate if the build arftifacts should be cleaned up before the build starts")]
        public static readonly string CleanupArtifactsBeforeBuildEnabled = "Arbor.X.Artifacts.CleanupBeforeBuildEnabled";

        [VariableDescription("Full build version number")]
        public static readonly string Version = Arbor.X.Build + ".Version";

        [VariableDescription("Max number of CPUs for MSBuild to use")]
        public static readonly string CpuLimit = "Arbor.X.CpuLimit";

        [VariableDescription(".NET assembly version")]
        public static readonly string NetAssemblyVersion = Arbor.X.Build + ".NetAssembly.Version";

        [VariableDescription(".NET assembly file version")]
        public static readonly string NetAssemblyFileVersion = Arbor.X.Build + ".NetAssembly.FileVersion";

        [VariableDescription("Enable assembly version patching")]
        public static readonly string AssemblyFilePatchingEnabled = Arbor.X.Build + ".NetAssembly.PatchingEnabled";

        [VariableDescription("Flag to indicate if the build is consider a release build")]
        public static readonly string ReleaseBuild = Arbor.X.Build + ".IsReleaseBuild";

        [VariableDescription("MSBuild configuration (eg. Debug/Release)")]
        public static readonly string Configuration = Arbor.X.Build + ".Configuration";

        [VariableDescription("Dynamic configuration property")]
        public static readonly string CurrentBuildConfiguration = Arbor.X.Build + ".CurrentBuild.Configuration";

        [VariableDescription("Temporary directory path")]
        public static readonly string TempDirectory = Arbor.X.Build + ".TempDirectory";

        [VariableDescription("Symbol server URI for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_Uri = "Arbor.X.Tools.External.SymbolServer.Uri";

        [VariableDescription("Symbol server API key for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_ApiKey = "Arbor.X.Tools.External.SymbolServer.ApiKey";

        [VariableDescription("Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public static readonly string ExternalTools_SymbolServer_ForceUploadEnabled = "Arbor.X.Tools.External.SymbolServer.ForceUploadEnabled";

        [VariableDescription("Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public const string ExternalTools_SymbolServer_UploadTimeoutInSeconds = "Arbor.X.NuGet.SymbolServer.TimeoutInSeconds";

        [VariableDescription("Flag to indicate that Symbol server package upload is enabled")]
        public static readonly string ExternalTools_SymbolServer_Enabled = "Arbor.X.Tools.External.SymbolServer.Enabled";

        [VariableDescription("Flag to indicate if the build is running on a build agent")]
        public static readonly string IsRunningOnBuildAgent = Arbor.X.Build + ".IsRunningOnBuildAgent";

        [VariableDescription("Flag to indicate if the bootstrapper is allowed to download pre-release versions of Arbor.X NuGet package", "false")]
        public static readonly string AllowPrerelease = Arbor.X.Build + ".Bootstrapper.AllowPrerelease";

        [VariableDescription("Arbor.X NuGet package version for bootstrapper to download")]
        public static readonly string ArborXNuGetPackageVersion = "Arbor.X.NuGetPackageVersion";

        [VariableDescription("NuGet source to use when downloading Arbor.X NuGet package")]
        public static readonly string ArborXNuGetPackageSource = "Arbor.X.NuGetPackage.Source";

        [VariableDescription("Flag to indicate if the bootstrapper should use -NoCache flag when downloading Arbor.X NuGet package")]
        public static readonly string ArborXNuGetPackageNoCacheEnabled = "Arbor.X.NuGetPackage.NoCachedEnabled";

        [VariableDescription("MSBuild executable path (eg. C:\\MSbuild.exe)")]
        public static readonly string ExternalTools_MSBuild_ExePath = "Arbor.X.Tools.External.MSBuild.ExePath";

        [VariableDescription("MSBuild max version")]
        public static readonly string ExternalTools_MSBuild_MaxVersion = "Arbor.X.Tools.External.MSBuild.MaxVersion";

        [VariableDescription("MSBuild verbosity level","normal")]
        public static readonly string ExternalTools_MSBuild_Verbosity = "Arbor.X.Tools.External.MSBuild.Verbosity";

        [VariableDescription("Flag to indicate if MSBuild should display a build summary","false")]
        public static readonly string ExternalTools_MSBuild_SummaryEnabled = "Arbor.X.Tools.External.MSBuild.SummaryEnabled";

        [VariableDescription("MSBuild build configuration, if not specified, all wellknown configurations will be built")]
        public static readonly string ExternalTools_MSBuild_BuildConfiguration = "Arbor.X.Tools.External.MSBuild.BuildConfiguration";

        [VariableDescription("MSBuild build platform, if not specified, all wellknown platforms will be built")]
        public static readonly string ExternalTools_MSBuild_BuildPlatform = "Arbor.X.Tools.External.MSBuild.BuildPlatform";

        [VariableDescription("Flag to indicate if code analysis should be run by MSBuild")]
        public static readonly string ExternalTools_MSBuild_CodeAnalysisEnabled = "Arbor.X.Tools.External.MSBuild.CodeAnalysis.Enabled";

        [VariableDescription("MSBuild detault target when building")]
        public static readonly string ExternalTools_MSBuild_DefaultTarget = "Arbor.X.Tools.External.MSBuild.DefaultTarget";

        [VariableDescription("Directory path for the current version control system repository")]
        public static readonly string SourceRoot = "SourceRoot";

        [VariableDescription("Flag to indicate if build platform AnyCPU is disabled","false", AnyCpuEnabled)]
        public static readonly string IgnoreAnyCpu = Arbor.X.Build + ".Platform.AnyCPU.Disabled";

        [VariableDescription("Flag to indicate if build platform AnyCPU is enabled")]
        public const string AnyCpuEnabled = "Arbor.X.Build.Platform.AnyCPU.Enabled";

        [VariableDescription("Flag to indicate if build configuration Release is enabled", "true")]
        public static readonly string ReleaseBuildEnabled = Arbor.X.Build + ".Configuration.Release.Enabled";

        [VariableDescription("Flag to indicate if build platform configuration Debug is enabled", "true")]
        public static readonly string DebugBuildEnabled = Arbor.X.Build + ".Configuration.Debug.Enabled";

        [VariableDescription("Flag to indicate if test runner error results are ignored", "false")]
        public static readonly string IgnoreTestFailures = Arbor.X.Build + ".Tests.IgnoreTestFailures";

        [VariableDescription("Test categories and tags to ignore, comma separated")]
        public static readonly string IgnoredTestCategories = Arbor.X.Build + ".Tests.IgnoredCategories";

        [VariableDescription("Flag to indicate if tests are enabled", "false")]
        public static readonly string TestsEnabled = Arbor.X.Build + ".Tests.Enabled";

        [VariableDescription("Visual Studio Test Framework console application path, (eg. C:\\VSTestConsole.exe)", "false")]
        public static readonly string ExternalTools_VSTest_ExePath = "Arbor.X.Tools.External.VSTest.ExePath";

        [VariableDescription("Visual Studio Test Framework test reports directory path")]
        public static readonly string ExternalTools_VSTest_ReportPath = "Arbor.X.Artifacts.TestReports.VSTest";

        [VariableDescription("Machine.Specifications reports directory path")]
        public static readonly string ExternalTools_MSpec_ReportPath = "Arbor.X.Artifacts.TestReports.MSpec";

        [VariableDescription("PDB artifacts enabled")]
        public static readonly string PublishPdbFilesAsArtifacts = "Arbor.X.Artifacts.PdbArtifacts.Enabled";

        [VariableDescription("ILMerge executable path (eg. C:\\ILRepack.exe)")]
        public static readonly string ExternalTools_ILRepack_ExePath = "Arbor.X.Tools.External.ILRepack.ExePath";

        [VariableDescription("ILMerge custom executable path (eg. C:\\ILRepack.exe)")]
        public static readonly string ExternalTools_ILRepack_Custom_ExePath = "Arbor.X.Tools.External.ILRepack.CustomExePath";

        [VariableDescription("Flag to indicate if Kudu deployment is enabled", "true")]
        public static readonly string ExternalTools_Kudu_Enabled = "Arbor.X.Tools.External.Kudu.Enabled";

        [VariableDescription("External, Kudu: deployment target directory path (website public directory)")]
        public static readonly string ExternalTools_Kudu_DeploymentTarget = "DEPLOYMENT_TARGET";

        [VariableDescription("External, Kudu: site running as x86 or x64 process")]
        public static readonly string ExternalTools_Kudu_Platform = "REMOTEDEBUGGINGBITVERSION";

        [VariableDescription("External, Kudu: deployment version control branch")]
        public const string ExternalTools_Kudu_DeploymentBranchName = "deployment_branch";

        [VariableDescription("Deployment branch to be used in Kudu, overrides value defined in " + ExternalTools_Kudu_DeploymentBranchName)]
        public static readonly string ExternalTools_Kudu_DeploymentBranchNameOverride = "Arbor.X.Tools.External.Kudu.DeploymentBranchNameOverride";

        [VariableDescription("External, Kudu: number of processors available on the current system")]
        public static readonly string ExternalTools_Kudu_ProcessorCount = "NUMBER_OF_PROCESSORS";

        [VariableDescription("Flag to indicate if Kudu WebJobs defined in App_Data directory is to be handled by the Kudu WebJobs aware tools")]
        public static readonly string AppDataJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.AppData.Enabled";

        [VariableDescription("MSBuild configuration to be used to locate web application artifacts to be deployed, if not found by the tools")]
        public static readonly string KuduConfigurationFallback = "Arbor.X.Tools.External.Kudu.ConfigurationFallback";

        [VariableDescription("Flag to indicate if Kudu WebJobs is to be handles by the Kudu WebJobs aware tools")]
        public static readonly string KuduJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.Enabled";

        [VariableDescription("Time out in seconds for total build process")]
        public static readonly string BuildToolTimeoutInSeconds = Arbor.X.Build + ".TimeoutInSeconds";

        [VariableDescription("Bootstrapper exit delay in milliseconds")]
        public static readonly string BootstrapperExitDelayInMilliseconds = "Arbor.X.Bootstrapper.ExitDelayInMilliseconds";

        [VariableDescription("Build application exit delay in milliseconds")]
        public static readonly string BuildApplicationExitDelayInMilliseconds = Arbor.X.Build + ".ExitDelayInMilliseconds";

        [VariableDescription("Flag to indicate if defined variables can be overriden")]
        public static readonly string VariableOverrideEnabled = Arbor.X.Build + ".VariableOverrideEnabled";

        [VariableDescription("Flag to indicate if a file arborx_environmentvariables.json should be used as a source to set environment variables")]
        public static readonly string VariableFileSourceEnabled = Arbor.X.Build + ".VariableFileSource.Enabled";

        [VariableDescription("Flag to indicate if Kudu target path files and directories should be deleted before deploy")]
        public static readonly string KuduClearFilesAndDirectories = "Arbor.X.Tools.External.Kudu.ClearEnabled";

        [VariableDescription("Flag to indicate if Kudu should use app_offline.htm file when deploying")]
        public static readonly string KuduUseAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.UseAppOfflineHtmFile";

        [VariableDescription("Flag to indicate if Kudu should exclude App_Data directory when deploying")]
        public static readonly string KuduExcludeDeleteAppData = "Arbor.X.Tools.External.Kudu.ExcludeDeleteApp_Data";

        [VariableDescription("Enable Machine.Specifications")]
        public static readonly string MSpecEnabled = "Arbor.X.Tools.External.MSpec.Enabled";

        [VariableDescription("Enable Machine.Specifications XSL transformation to NUnit")]
        public static readonly string MSpecJUnitXslTransformationEnabled = "Arbor.X.Tools.External.MSpec.JUnitXslTransformation.Enabled";

        [VariableDescription("Enable NUnit")]
        public static readonly string NUnitEnabled = "Arbor.X.Tools.External.NUnit.Enabled";

        [VariableDescription("Enable VSTest")]
        public static readonly string VSTestEnabled = "Arbor.X.Tools.External.VSTest.Enabled";

        [VariableDescription("'|' (bar) separated list of file names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteFiles = "Arbor.X.Tools.External.Kudu.IgnoreDeleteFilesBarSeparatedList";

        [VariableDescription("'|' (bar) separated list of directory names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteDirectories = "Arbor.X.Tools.External.Kudu.IgnoreDeleteDirectoriesBarSeparatedList";

        [VariableDescription("Site for Kudu to deploy, needs to be specified if there are multiple web projects. Name of the csproj file exception the extension.")]
        public static readonly string KuduSiteToDeploy = "Arbor.X.Tools.External.Kudu.SiteToDeploy";

        [VariableDescription("Flag to indicate if Kudu should delete any existing app_offline.htm file when deploying")]
        public static readonly string KuduDeleteExistingAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.DeleteExistingAppOfflineHtmFile";

        [VariableDescription("Log level")]
        public static readonly string LogLevel = "Arbor.X.Log.Level";

        [VariableDescription("Generic XML transformaions enabled")]
        public static readonly string GenericXmlTransformsEnabled = "Arbor.X.Build.XmlTransformations.Enabled";

        [VariableDescription("Run tests in release configuration")]
        public static readonly string RunTestsInReleaseConfigurationEnabled = "Arbor.X.Tests.RunTestsInReleaseConfiguration";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public static readonly string CleanBinXmlFilesForAssembliesEnabled = "Arbor.X.Build.WebApplications.CleanBinXmlFilesForAssembliesEnabled";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public static readonly string CleanWebJobsXmlFilesForAssembliesEnabled = "Arbor.X.Build.WebApplications.WebJobs.CleanWebJobsXmlFilesForAssembliesEnabled";

        [VariableDescription("List of file name parts to be used when excluding files from being copied to web jobs directory, comma separated")]
        public static readonly string WebJobsExcludedFileNameParts = "Arbor.X.Build.WebApplications.WebJobs.ExcludedFileNameParts";

        [VariableDescription("List of file name parts to be used when excluding directories from being copied to web jobs directory, comma separated")]
        public static readonly string WebJobsExcludedDirectorySegments = "Arbor.X.Build.WebApplications.WebJobs.ExcludedDirectorySegments";

        public const string DotNetRestoreEnabled = "Arbor.X.DotNet.Restore.Enabled";

        public const string DotNetExePath = "Arbor.X.DotNet.ExePath";

        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore InconsistentNaming

        public static IReadOnlyCollection<VariableDescription> AllVariables
        {
            get
            {
                var allVariables = new List<VariableDescription>();

                List<FieldInfo> fields = typeof (WellKnownVariables).GetTypeInfo().GetFields().Where(field => field.IsPublicConstantOrStatic()).ToList();

                foreach (var field in fields)
                {
                    var invariantName = (string) field.GetValue(null);

                    VariableDescriptionAttribute attribute = field.GetCustomAttribute<VariableDescriptionAttribute>();

                    VariableDescription description = attribute != null
                        ? VariableDescription.Create(invariantName, attribute.Description, field.Name, attribute.DefaultValue)
                        : VariableDescription.Create(field.Name);

                    allVariables.Add(description);
                }

                return allVariables.OrderBy(name => name.InvariantName).ToList();
            }
        }
    }
}
