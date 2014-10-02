﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
// ReSharper disable InconsistentNaming

// ReSharper disable ConvertToConstant.Global
        

        [VariableDescription("Visual Studio version")]
        public static readonly string ExternalTools_VisualStudio_Version =
            "Arbor.X.Tools.External.VisualStudio.Version";

        [VariableDescription("Build arftifacts path")]
        public static readonly string Artifacts = "Arbor.X.Artifacts";

        [VariableDescription("Full build version number")]
        public static readonly string Version = Arbor.X.Build + ".Version";
        
        [VariableDescription("External tools path")]
        public static readonly string DirectoryCloneEnabled = "Arbor.X.Vcs.DirectoryCloneEnabled";

        [VariableDescription("Max number of CPUs for MSBuild to use")]
        public static readonly string CpuLimit = "Arbor.X.CpuLimit";

        [VariableDescription(".NET assembly version")]
        public static readonly string NetAssemblyVersion = Arbor.X.Build + ".NetAssembly.Version";

        [VariableDescription(".NET assembly file version")]
        public static readonly string NetAssemblyFileVersion = Arbor.X.Build + ".NetAssembly.FileVersion";


        [VariableDescription("Flag to indicate if NuGet package creation is enabled")]
        public static readonly string NuGetPackageEnabled = Arbor.X + ".NuGet.Package.Enabled";

        [VariableDescription("NuGet package artifacts suffix")]
        public static readonly string NuGetPackageArtifactsSuffix = Arbor.X + ".NuGet.Package.Artifacts.Suffix";

        [VariableDescription("Flag to indicate if the build number is included in the NuGet package artifacts")]
        public static readonly string BuildNumberInNuGetPackageArtifactsEnabled = Arbor.X + ".NuGet.Package.Artifacts.BuildNumber.Enabled";

        [VariableDescription("Enable assembly version patching")]
        public static readonly string AssemblyFilePatchingEnabled = Arbor.X.Build + ".NetAssembly.PatchingEnabled";

        [VariableDescription("Flag to indicate if the build is consider a release build")]
        public static readonly string ReleaseBuild = Arbor.X.Build + ".IsReleaseBuild";

        [VariableDescription("MSBuild configuration (eg. Debug/Release)")]
        public static readonly string Configuration = Arbor.X.Build + ".Configuration";

        [VariableDescription("Current branch name for the version control system")]
        public static readonly string BranchName = "Arbor.X.Vcs.Branch.Name";

        [VariableDescription("Temporary directory path")]
        public static readonly string TempDirectory = Arbor.X.Build + ".TempDirectory";

        [VariableDescription("NuGet executable path (eg. C:\\nuget.exe)")]
        public static readonly string ExternalTools_NuGet_ExePath = "Arbor.X.Tools.External.NuGet.ExePath";

        [VariableDescription("Symbol server URI for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_Uri = "Arbor.X.Tools.External.SymbolServer.Uri";

        [VariableDescription("Symbol server API key for NuGet source package upload")]
        public static readonly string ExternalTools_SymbolServer_ApiKey = "Arbor.X.Tools.External.SymbolServer.ApiKey";

        [VariableDescription("Flag to indicate if the build is running on a build agent")]
        public static readonly string IsRunningOnBuildAgent = Arbor.X.Build + ".IsRunningOnBuildAgent";

        [VariableDescription("Flag to indicate if the bootstrapper is allowed to download pre-release versions of Arbor.X NuGet package", "false")]
        public static readonly string AllowPrerelease = Arbor.X.Build + ".Bootstrapper.AllowPrerelease";

        [VariableDescription("Arbor.X NuGet package version for bootstrapper to download", "false")]
        public static readonly string ArborXNuGetPackageVersion = "Arbor.X.NuGetPackageVersion";
                
        [VariableDescription("MSBuild executable path (eg. C:\\MSbuild.exe)")]
        public static readonly string ExternalTools_MSBuild_ExePath = "Arbor.X.Tools.External.MSBuild.ExePath";

        [VariableDescription("MSBuild verbosity level","normal")]
        public static readonly string ExternalTools_MSBuild_Verbosity = "Arbor.X.Tools.External.MSBuild.Verbosity";

        [VariableDescription("Flag to indicate if MSBuild should display a build summary","false")]
        public static readonly string ExternalTools_MSBuild_SummaryEnabled = "Arbor.X.Tools.External.MSBuild.SummaryEnabled";

        [VariableDescription("MSBuild build configuration, if not specified, all wellknown configurations will be built")]
        public static readonly string ExternalTools_MSBuild_BuildConfiguration = "Arbor.X.Tools.External.MSBuild.BuildConfiguration";
        
        [VariableDescription("MSBuild build platform, if not specified, all wellknown platforms will be built")]
        public static readonly string ExternalTools_MSBuild_BuildPlatform = "Arbor.X.Tools.External.MSBuild.BuildPlatform";

        [VariableDescription("Directory path for the current version control system repository")]
        public static readonly string SourceRoot = "SourceRoot";

        [VariableDescription("Flag to indicate if build platform AnyCPU is disabled","false", AnyCpuEnabled)]
        public static readonly string IgnoreAnyCpu = Arbor.X.Build + ".Platform.AnyCPU.Disabled";

        [VariableDescription("Flag to indicate if build platform AnyCPU is enabled")]
        public const string AnyCpuEnabled = "Arbor.X.Build.Platform.AnyCPU.Enabled";

        [VariableDescription("Flag to indicate if build configuration Release is disabled", "false")]
        public static readonly string IgnoreRelease = Arbor.X.Build + ".Configuration.Release.Disabled";

        [VariableDescription("Flag to indicate if build platform configuration Debug is disabled", "false")]
        public static readonly string IgnoreDebug = Arbor.X.Build + ".Configuration.Debug.Disabled";

        [VariableDescription("Flag to indicate if test runner error results are ignored", "false")]
        public static readonly string IgnoreTestFailures = Arbor.X.Build + ".Tests.IgnoreTestFailures";

        [VariableDescription("Flag to indicate if tests are enabled", "false")]
        public static readonly string TestsEnabled = Arbor.X.Build + ".Tests.Enabled";

        [VariableDescription("Visual Studio Test Framework console application path, (eg. C:\\VSTestConsole.exe)", "false")]
        public static readonly string ExternalTools_VSTest_ExePath = "Arbor.X.Tools.External.VSTest.ExePath";

        [VariableDescription("Visual Studio Test Framework test reports directory path")]
        public static readonly string ExternalTools_VSTest_ReportPath = "Arbor.X.Artifacts.TestReports.VSTest";

        [VariableDescription("Machine.Specifications reports directory path")]
        public static readonly string ExternalTools_MSpec_ReportPath = "Arbor.X.Artifacts.TestReports.MSpec";
        
        [VariableDescription("ILMerge executable path (eg. C:\\IlMerge.exe)")]
        public static readonly string ExternalTools_ILMerge_ExePath = "Arbor.X.Tools.External.ILMerge.ExePath";

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

        [VariableDescription("Flag to indicate if Kudu target path files and directories should be deleted before deploy")]
        public static readonly string KuduClearFilesAndDirectories = "Arbor.X.Tools.External.Kudu.ClearEnabled";
  
        [VariableDescription("Flag to indicate if Kudu should use app_offline.htm file when deploying")]
        public static readonly string KuduUseAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.UseAppOfflineHtmFile";

        [VariableDescription("Flag to indicate if Kudu should exclude App_Data directory when deploying")]
        public static readonly string KuduExcludeDeleteAppData = "Arbor.X.Tools.External.Kudu.ExcludeDeleteApp_Data";
        
        [VariableDescription("Enable Machine.Specifications")]
        public static readonly string MSpecEnabled = "Arbor.X.Tools.External.MSpec.Enabled";
        
        [VariableDescription("Enable NUnit")]
        public static readonly string NUnitEnabled = "Arbor.X.Tools.External.NUnit.Enabled";

        [VariableDescription("Enable VSTest")]
        public static readonly string VSTestEnabled = "Arbor.X.Tools.External.VSTest.Enabled";

        [VariableDescription("'|' (bar) separated list of file names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteFiles = "Arbor.X.Tools.External.Kudu.IgnoreDeleteFilesBarSeparatedList";

        [VariableDescription("'|' (bar) separated list of directory names to not delete when deploying")]
        public static readonly string KuduIgnoreDeleteDirectories = "Arbor.X.Tools.External.Kudu.IgnoreDeleteDirectoriesBarSeparatedList";

        [VariableDescription("Flag to indicate if Kudu should delete any existing app_offline.htm file when deploying")]
        public static readonly string KuduDeleteExistingAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.DeleteExistingAppOfflineHtmFile";

        [VariableDescription("Log level")]
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