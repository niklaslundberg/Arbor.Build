using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Arbor.X.Core.BuildVariables
{
    public static class WellKnownVariables
    {
// ReSharper disable InconsistentNaming

// ReSharper disable ConvertToConstant.Global
        [VariableDescriptionAttribute("External tools path")]
        public static readonly string ExternalTools = Arbor.X.Build + ".Tools.External";

        [VariableDescriptionAttribute("Source root override")]
        public static readonly string SourceRootOverride = Arbor.X.Build + ".Source.Override";

        [VariableDescriptionAttribute("Test framework report path")]
        public static readonly string ReportPath = "Arbor.X.Artifacts.TestReports";

        [VariableDescriptionAttribute("Visual Studio version")]
        public static readonly string ExternalTools_VisualStudio_Version =
            "Arbor.X.Tools.External.VisualStudio.Version";

        [VariableDescriptionAttribute("Build arftifacts path")]
        public static readonly string Artifacts = "Arbor.X.Artifacts";

        [VariableDescriptionAttribute("Full build version number")]
        public static readonly string Version = Arbor.X.Build + ".Version";
        
        [VariableDescriptionAttribute("External tools path")]
        public static readonly string DirectoryCloneEnabled = "Arbor.X.Vcs.DirectoryCloneEnabled";

        [VariableDescriptionAttribute("Max number of CPUs for MSBuild to use")]
        public static readonly string CpuLimit = "Arbor.X.CpuLimit";

        [VariableDescriptionAttribute(".NET assembly version")]
        public static readonly string NetAssemblyVersion = Arbor.X.Build + ".NetAssembly.Version";

        [VariableDescriptionAttribute(".NET assembly file version")]
        public static readonly string NetAssemblyFileVersion = Arbor.X.Build + ".NetAssembly.FileVersion";

        [VariableDescriptionAttribute("NuGet package artifacts suffix")]
        public static readonly string NuGetPackageArtifactsSuffix = Arbor.X + ".NuGet.Package.Artifacts.Suffix";

        [VariableDescriptionAttribute("Flag to indicate if the build number is included in the NuGet package artifacts")]
        public static readonly string BuildNumberInNuGetPackageArtifactsEnabled = Arbor.X + ".NuGet.Package.Artifacts.BuildNumber.Enabled";

        [VariableDescriptionAttribute("Enable assembly version patching")]
        public static readonly string AssemblyFilePatchingEnabled = Arbor.X.Build + ".NetAssembly.PatchingEnabled";

        [VariableDescriptionAttribute("Flag to indicate if the build is consider a release build")]
        public static readonly string ReleaseBuild = Arbor.X.Build + ".IsReleaseBuild";

        [VariableDescriptionAttribute("MSBuild configuration (eg. Debug/Release)")]
        public static readonly string Configuration = Arbor.X.Build + ".Configuration";

        [VariableDescriptionAttribute("Current branch name for the version control system")]
        public static readonly string BranchName = "Arbor.X.Vcs.Branch.Name";

        [VariableDescriptionAttribute("Temporary directory path")]
        public static readonly string TempDirectory = Arbor.X.Build + ".TempDirectory";

        [VariableDescriptionAttribute("NuGet executable path (eg. C:\\nuget.exe)")]
        public static readonly string ExternalTools_NuGet_ExePath = "Arbor.X.Tools.External.NuGet.ExePath";

        [VariableDescriptionAttribute("Symbol server URI for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_Uri = "Arbor.X.Tools.External.SymbolServer.Uri";

        [VariableDescriptionAttribute("Symbol server API key for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_ApiKey = "Arbor.X.Tools.External.SymbolServer.ApiKey";

        [VariableDescriptionAttribute("Flag to indicate if the build is running on a build agent")]
        public static readonly string IsRunningOnBuildAgent = Arbor.X.Build + ".IsRunningOnBuildAgent";

        [VariableDescriptionAttribute("Flag to indicate if the bootstrapper is allowed to download pre-release versions of Arbor.X NuGet package", "false")]
        public static readonly string AllowPrerelease = Arbor.X.Build + ".Bootstrapper.AllowPrerelease";

        [VariableDescriptionAttribute("Arbor.X NuGet package version for bootstrapper to download", "false")]
        public static readonly string ArborXNuGetPackageVersion = "Arbor.X.NuGetPackageVersion";
                
        [VariableDescriptionAttribute("MSBuild executable path (eg. C:\\MSbuild.exe)")]
        public static readonly string ExternalTools_MSBuild_ExePath = "Arbor.X.Tools.External.MSBuild.ExePath";

        [VariableDescriptionAttribute("MSBuild verbosity level","normal")]
        public static readonly string ExternalTools_MSBuild_Verbosity = "Arbor.X.Tools.External.MSBuild.Verbosity";

        [VariableDescriptionAttribute("Flag to indicate if MSBuild should display a build summary","false")]
        public static readonly string ExternalTools_MSBuild_SummaryEnabled = "Arbor.X.Tools.External.MSBuild.SummaryEnabled";

        [VariableDescriptionAttribute("MSBuild build configuration, if not specified, all wellknown configurations will be built")]
        public static readonly string ExternalTools_MSBuild_BuildConfiguration = "Arbor.X.Tools.External.MSBuild.BuildConfiguration";
        
        [VariableDescriptionAttribute("MSBuild build platform, if not specified, all wellknown platforms will be built")]
        public static readonly string ExternalTools_MSBuild_BuildPlatform = "Arbor.X.Tools.External.MSBuild.BuildPlatform";

        [VariableDescriptionAttribute("Directory path for the current version control system repository")]
        public static readonly string SourceRoot = "SourceRoot";

        [VariableDescriptionAttribute("Flag to indicate if build platform AnyCPU is disabled","false", AnyCpuEnabled)]
        public static readonly string IgnoreAnyCpu = Arbor.X.Build + ".Platform.AnyCPU.Disabled";

        [VariableDescriptionAttribute("Flag to indicate if build platform AnyCPU is enabled")]
        public const string AnyCpuEnabled = "Arbor.X.Build.Platform.AnyCPU.Enabled";

        [VariableDescriptionAttribute("Flag to indicate if build configuration Release is disabled", "false")]
        public static readonly string IgnoreRelease = Arbor.X.Build + ".Configuration.Release.Disabled";

        [VariableDescriptionAttribute("Flag to indicate if build platform configuration Debug is disabled", "false")]
        public static readonly string IgnoreDebug = Arbor.X.Build + ".Configuration.Debug.Disabled";

        [VariableDescriptionAttribute("Flag to indicate if test runner error results are ignored", "false")]
        public static readonly string IgnoreTestFailures = Arbor.X.Build + ".Tests.IgnoreTestFailures";

        [VariableDescriptionAttribute("Flag to indicate if tests are enabled", "false")]
        public static readonly string TestsEnabled = Arbor.X.Build + ".Tests.Enabled";

        [VariableDescriptionAttribute("Visual Studio Test Framework console application path, (eg. C:\\VSTestConsole.exe)", "false")]
        public static readonly string ExternalTools_VSTest_ExePath = "Arbor.X.Tools.External.VSTest.ExePath";

        [VariableDescriptionAttribute("Visual Studio Test Framework test reports directory path")]
        public static readonly string ExternalTools_VSTest_ReportPath = "Arbor.X.Artifacts.TestReports.VSTest";

        [VariableDescriptionAttribute("Machine.Specifications reports directory path")]
        public static readonly string ExternalTools_MSpec_ReportPath = "Arbor.X.Artifacts.TestReports.MSpec";
        
        [VariableDescriptionAttribute("ILMerge executable path (eg. C:\\IlMerge.exe)")]
        public static readonly string ExternalTools_ILMerge_ExePath = "Arbor.X.Tools.External.ILMerge.ExePath";

        [VariableDescriptionAttribute("Flag to indicate if Kudu deployment is enabled", "true")]
        public static readonly string ExternalTools_Kudu_Enabled = "Arbor.X.Tools.External.Kudu.Enabled";

        [VariableDescriptionAttribute("External, Kudu: deployment target directory path (website public directory)")]
        public static readonly string ExternalTools_Kudu_DeploymentTarget = "DEPLOYMENT_TARGET";

        [VariableDescriptionAttribute("External, Kudu: site running as x86 or x64 process")]
        public static readonly string ExternalTools_Kudu_Platform = "REMOTEDEBUGGINGBITVERSION";

        [VariableDescriptionAttribute("External, Kudu: deployment version control branch")]
        public const string ExternalTools_Kudu_DeploymentBranchName = "deployment_branch";

        [VariableDescriptionAttribute("Deployment branch to be used in Kudu, overrides value defined in " + ExternalTools_Kudu_DeploymentBranchName)]
        public static readonly string ExternalTools_Kudu_DeploymentBranchNameOverride = "Arbor.X.Tools.External.Kudu.DeploymentBranchNameOverride";

        [VariableDescriptionAttribute("External, Kudu: number of processors available on the current system")]
        public static readonly string ExternalTools_Kudu_ProcessorCount = "NUMBER_OF_PROCESSORS";
        
        [VariableDescriptionAttribute("Flag to indicate if Kudu WebJobs defined in App_Data directory is to be handled by the Kudu WebJobs aware tools")]
        public static readonly string AppDataJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.AppData.Enabled";

        [VariableDescriptionAttribute("MSBuild configuration to be used to locate web application artifacts to be deployed, if not found by the tools")]
        public static readonly string KuduConfigurationFallback = "Arbor.X.Tools.External.Kudu.ConfigurationFallback";

        [VariableDescriptionAttribute("Flag to indicate if Kudu WebJobs is to be handles by the Kudu WebJobs aware tools")]
        public static readonly string KuduJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.Enabled";

        [VariableDescriptionAttribute("Time out in seconds for total build process")]
        public static readonly string BuildToolTimeoutInSeconds = Arbor.X.Build + ".TimeoutInSeconds";

        [VariableDescriptionAttribute("Bootstrapper exit delay in milliseconds")]
        public static readonly string BootstrapperExitDelayInMilliseconds = "Arbor.X.Bootstrapper.ExitDelayInMilliseconds";

        [VariableDescriptionAttribute("Build application exit delay in milliseconds")]
        public static readonly string BuildApplicationExitDelayInMilliseconds = Arbor.X.Build + ".ExitDelayInMilliseconds";

        [VariableDescriptionAttribute("Flag to indicate if defined variables can be overriden")]
        public static readonly string VariableOverrideEnabled = Arbor.X.Build + ".VariableOverrideEnabled";

        [VariableDescriptionAttribute("Flag to indicate if Kudu target path files and directories should be deleted before deploy")]
        public static readonly string KuduClearFilesAndDirectories = "Arbor.X.Tools.External.Kudu.ClearEnabled";
  
        [VariableDescriptionAttribute("Flag to indicate if Kudu should use app_offline.htm file when deploying")]
        public static readonly string KuduUseAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.UseAppOfflineHtmFile";

        [VariableDescriptionAttribute("Flag to indicate if Kudu should exclude App_Data directory when deploying")]
        public static readonly string KuduExcludeDeleteAppData = "Arbor.X.Tools.External.Kudu.ExcludeDeleteApp_Data";
        
        [VariableDescriptionAttribute("Enable Machine.Specifications")]
        public static readonly string MSpecEnabled = "Arbor.X.Tools.External.MSpec.Enabled";
        
        [VariableDescriptionAttribute("Enable NUnit")]
        public static readonly string NUnitEnabled = "Arbor.X.Tools.External.NUnit.Enabled";

        [VariableDescriptionAttribute("Enable VSTest")]
        public static readonly string VSTestEnabled = "Arbor.X.Tools.External.VSTest.Enabled";

        [VariableDescriptionAttribute("'|' (bar) separated list of file names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteFiles = "Arbor.X.Tools.External.Kudu.IgnoreDeleteFilesBarSeparatedList";

        [VariableDescriptionAttribute("'|' (bar) separated list of directory names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteDirectories = "Arbor.X.Tools.External.Kudu.IgnoreDeleteDirectoriesBarSeparatedList";

        [VariableDescriptionAttribute("Flag to indicate if Kudu should delete any existing app_offline.htm file when deploying")]
        public static readonly string KuduDeleteExistingAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.DeleteExistingAppOfflineHtmFile";

        [VariableDescriptionAttribute("Log level")]
        public static readonly string LogLevel = "Arbor.X.Log.Level";

        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore InconsistentNaming

        public static IReadOnlyCollection<VariableDescription> AllVariables
        {
            get
            {
                var allVariables = new List<VariableDescription>();

                var fields = typeof (WellKnownVariables).GetTypeInfo().GetFields().Where(field => (field.IsLiteral  || field.IsStatic) && field.IsPublic).ToList();

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