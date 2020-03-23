using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Arbor.Build.Core.GenericExtensions;

namespace Arbor.Build.Core.BuildVariables
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1310:Field names must not contain underscore",
        Justification = "Variables")]
    public static partial class WellKnownVariables
    {
        // ReSharper disable InconsistentNaming

        // ReSharper disable ConvertToConstant.Global

        [VariableDescription("Flag to enabled dotnet publish for executable netcoreapp projects")]
        public const string DotNetPublishExeProjectsEnabled = "Arbor.Build.Build.PublishDotNetExecutableProjects";

        [VariableDescription("Flag to enabled dotnet publish for executable netcoreapp projects")]
        public const string DotNetPublishExeEnabled = "ArborBuild_PublishDotNetExecutableEnabled";

        [VariableDescription("Flag to enabled dotnet pack for tool netcoreapp projects")]
        public const string DotNetPackToolProjectsEnabled = "Arbor.Build.Build.PackDotNetToolProjects";

        [VariableDescription("Flag to indicate if build platform AnyCPU is enabled")]
        public const string AnyCpuEnabled = "Arbor.Build.Build.Platform.AnyCPU.Enabled";

        [VariableDescription(
            "Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public const string ExternalTools_SymbolServer_UploadTimeoutInSeconds =
            "Arbor.Build.NuGet.SymbolServer.TimeoutInSeconds";

        [VariableDescription("External, Kudu: deployment version control branch")]
        public const string ExternalTools_Kudu_DeploymentBranchName = "deployment_branch";

        public const string DotNetRestoreEnabled = "Arbor.Build.DotNet.Restore.Enabled";

        public const string MSBuildNuGetRestoreEnabled = "Arbor.Build.MSBuild.NuGetRestore.Enabled";

        public const string DotNetExePath = "Arbor.Build.DotNet.ExePath";

        [VariableDescription("Visual Studio version")]
        public const string ExternalTools_VisualStudio_Version =
            "Arbor.Build.Tools.External.VisualStudio.Version";

        [VariableDescription("Visual Studio version")]
        public const string ExternalTools_VisualStudio_Version_Allow_PreRelease =
            "Arbor.Build.Tools.External.VisualStudio.Version.PreRelease.Enabled";

        [VariableDescription("Build arftifacts path")]
        public const string Artifacts = "Arbor.Build.Artifacts";

        [VariableDescription("Flag to indicate if the build arftifacts should be cleaned up before the build starts")]
        public const string CleanupArtifactsBeforeBuildEnabled =
            "Arbor.Build.Artifacts.CleanupBeforeBuildEnabled";

        [VariableDescription("Full build version number")]
        public const string Version =
            "Arbor.Build.Build.Version";

        [VariableDescription("Max number of CPUs for MSBuild to use")]
        public const string CpuLimit =
            "Arbor.Build.CpuLimit";

        [VariableDescription(".NET assembly version")]
        public const string NetAssemblyVersion =
            "Arbor.Build.Build.NetAssembly.Version";

        [VariableDescription(".NET assembly file version")]
        public const string NetAssemblyFileVersion =
            "Arbor.Build.Build.NetAssembly.FileVersion";

        [VariableDescription("Enable assembly version patching")]
        public const string AssemblyFilePatchingEnabled = "Arbor.Build.Build.NetAssembly.PatchingEnabled";

        [VariableDescription("Flag to indicate if the build is consider a release build")]
        public const string ReleaseBuild = "Arbor.Build.Build.IsReleaseBuild";

        [VariableDescription("MSBuild configuration (eg. Debug/Release)")]
        public const string Configuration =
            "Arbor.Build.Build.Configuration";

        [VariableDescription("Default MSBuild configuration (eg. Debug/Release) for feature branches")]
        public const string FeatureBranchDefaultConfiguration =
            "Arbor.Build.FeatureBranchDefaultConfiguration";

        [VariableDescription("Dynamic configuration property")]
        public const string CurrentBuildConfiguration = "Arbor.Build.Build.CurrentBuild.Configuration";

        [VariableDescription("Temporary directory path")]
        public const string TempDirectory =
            "Arbor.Build.Build.TempDirectory";

        [VariableDescription("Symbol server URI for NuGet source package upload")]
        public const string ExternalTools_SymbolServer_Uri = "Arbor.Build.Tools.External.SymbolServer.Uri";

        [VariableDescription("Symbol server API key for NuGet source package upload")]
        public const string ExternalTools_SymbolServer_ApiKey = "Arbor.Build.Tools.External.SymbolServer.ApiKey";

        [VariableDescription(
            "Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public const string ExternalTools_SymbolServer_ForceUploadEnabled =
            "Arbor.Build.Tools.External.SymbolServer.ForceUploadEnabled";

        [VariableDescription("Flag to indicate that Symbol server package upload is enabled")]
        public const string ExternalTools_SymbolServer_Enabled =
            "Arbor.Build.Tools.External.SymbolServer.Enabled";

        [VariableDescription("Flag to indicate if the build is running on a build agent")]
        public const string IsRunningOnBuildAgent = "Arbor.Build.Build.IsRunningOnBuildAgent";

        [VariableDescription("Flag to indicate if the console log should include timestamps")]
        public const string ConsoleLogTimestampEnabled = "Arbor.Build.Build.Logging.Console.Timestamps.Enabled";

        [VariableDescription(
            "Flag to indicate if the bootstrapper is allowed to download pre-release versions of Arbor.Build NuGet package",
            "false")]
        public const string AllowPrerelease = "Arbor.Build.Build.Bootstrapper.AllowPrerelease";

        [VariableDescription("Arbor.Build NuGet package version for bootstrapper to download")]
        public const string ArborBuildNuGetPackageVersion = "Arbor.Build.NuGetPackageVersion";

        [VariableDescription("NuGet source to use when downloading Arbor.Build NuGet package")]
        public const string ArborBuildNuGetPackageSource = "Arbor.Build.NuGetPackage.Source";

        [VariableDescription("MSBuild executable path (eg. C:\\MSbuild.exe)")]
        public const string ExternalTools_MSBuild_ExePath = "Arbor.Build.Tools.External.MSBuild.ExePath";

        [VariableDescription("MSBuild max version")]
        public const string ExternalTools_MSBuild_MaxVersion =
            "Arbor.Build.Tools.External.MSBuild.MaxVersion";

        [VariableDescription("MSBuild max version")]
        public const string ExternalTools_MSBuild_AllowPrereleaseEnabled =
            "Arbor.Build.Tools.External.MSBuild.AllowPrerelease.Enabled";

        [VariableDescription("MSBuild verbosity level", "normal")]
        public const string ExternalTools_MSBuild_Verbosity = "Arbor.Build.Tools.External.MSBuild.Verbosity";

        [VariableDescription("Flag to indicate if MSBuild should display a build summary", "false")]
        public const string ExternalTools_MSBuild_SummaryEnabled =
            "Arbor.Build.Tools.External.MSBuild.SummaryEnabled";

        [VariableDescription(
            "MSBuild build configuration, if not specified, all wellknown configurations will be built")]
        public const string ExternalTools_MSBuild_BuildConfiguration =
            "Arbor.Build.Tools.External.MSBuild.BuildConfiguration";

        [VariableDescription("MSBuild build platform, if not specified, all wellknown platforms will be built")]
        public const string ExternalTools_MSBuild_BuildPlatform =
            "Arbor.Build.Tools.External.MSBuild.BuildPlatform";

        [VariableDescription("Flag to indicate if code analysis should be run by MSBuild")]
        public const string ExternalTools_MSBuild_CodeAnalysisEnabled =
            "Arbor.Build.Tools.External.MSBuild.CodeAnalysis.Enabled";

        [VariableDescription("MSBuild detault target when building")]
        public const string ExternalTools_MSBuild_DefaultTarget =
            "Arbor.Build.Tools.External.MSBuild.DefaultTarget";

        [VariableDescription("Directory path for the current version control system repository")]
        public const string SourceRoot = "SourceRoot";

        [VariableDescription("Flag to indicate if build platform AnyCPU is disabled", "false", AnyCpuEnabled)]
        public const string IgnoreAnyCpu = "Arbor.Build.Build.Platform.AnyCPU.Disabled";

        [VariableDescription("Flag to indicate if build configuration Release is enabled", "true")]
        public const string ReleaseBuildEnabled = "Arbor.Build.Build.Configuration.Release.Enabled";

        [VariableDescription("Flag to indicate if build platform configuration Debug is enabled", "true")]
        public const string DebugBuildEnabled = "Arbor.Build.Build.Configuration.Debug.Enabled";

        [VariableDescription(
            "Comma separated list to filter assemblies, to only run tests dlls starting with prefix, case insensitive",
            "")]
        public const string TestsAssemblyStartsWith = "Arbor.Build.Build.Tests.AssemblyStartsWith";

        [VariableDescription("Flag to indicate if tests are enabled", "false")]
        public const string TestsEnabled = "Arbor.Build.Build.Tests.Enabled";

        [VariableDescription(
            "Visual Studio Test Framework console application path, (eg. C:\\VSTestConsole.exe)",
            "false")]
        public const string ExternalTools_VSTest_ExePath =
            "Arbor.Build.Tools.External.VSTest.ExePath";

        [VariableDescription("PDB artifacts enabled")]
        public const string PublishPdbFilesAsArtifacts =
            "Arbor.Build.Artifacts.PdbArtifacts.Enabled";

        [VariableDescription("Flag to indicate if Kudu deployment is enabled", "true")]
        public const string ExternalTools_Kudu_Enabled = "Arbor.Build.Tools.External.Kudu.Enabled";

        [VariableDescription("External, Kudu: deployment target directory path (website public directory)")]
        public const string ExternalTools_Kudu_DeploymentTarget = "DEPLOYMENT_TARGET";

        [VariableDescription("External, Kudu: site running as x86 or x64 process")]
        public const string ExternalTools_Kudu_Platform = "REMOTEDEBUGGINGBITVERSION";

        [VariableDescription("Deployment branch to be used in Kudu, overrides value defined in " +
                             ExternalTools_Kudu_DeploymentBranchName)]
        public const string ExternalTools_Kudu_DeploymentBranchNameOverride =
            "Arbor.Build.Tools.External.Kudu.DeploymentBranchNameOverride";

        [VariableDescription("External, Kudu: number of processors available on the current system")]
        public const string ExternalTools_Kudu_ProcessorCount = "NUMBER_OF_PROCESSORS";

        [VariableDescription(
            "Flag to indicate if Kudu WebJobs defined in App_Data directory is to be handled by the Kudu WebJobs aware tools")]
        public const string AppDataJobsEnabled = "Arbor.Build.Tools.External.Kudu.WebJobs.AppData.Enabled";

        [VariableDescription(
            "MSBuild configuration to be used to locate web application artifacts to be deployed, if not found by the tools")]
        public const string KuduConfigurationFallback = "Arbor.Build.Tools.External.Kudu.ConfigurationFallback";

        [VariableDescription("Flag to indicate if Kudu WebJobs is to be handles by the Kudu WebJobs aware tools")]
        public const string KuduJobsEnabled = "Arbor.Build.Tools.External.Kudu.WebJobs.Enabled";

        [VariableDescription("Time out in seconds for total build process")]
        public const string BuildToolTimeoutInSeconds = "Arbor.Build.Build.TimeoutInSeconds";

        [VariableDescription("Bootstrapper exit delay in milliseconds")]
        public const string BootstrapperExitDelayInMilliseconds =
            "Arbor.Build.Bootstrapper.ExitDelayInMilliseconds";

        [VariableDescription("Build application exit delay in milliseconds")]
        public const string BuildApplicationExitDelayInMilliseconds =
            "Arbor.Build.Build.ExitDelayInMilliseconds";

        [VariableDescription("Flag to indicate if defined variables can be overriden")]
        public const string VariableOverrideEnabled = "Arbor.Build.Build.VariableOverrideEnabled";

        [VariableDescription(
            "Flag to indicate if a file arborbuild_environmentvariables.json should be used as a source to set environment variables")]
        public const string VariableFileSourceEnabled = "Arbor.Build.Build.VariableFileSource.Enabled";

        [VariableDescription(
            "Flag to indicate if Kudu target path files and directories should be deleted before deploy")]
        public const string KuduClearFilesAndDirectories = "Arbor.Build.Tools.External.Kudu.ClearEnabled";

        [VariableDescription("Flag to indicate if Kudu should use app_offline.htm file when deploying")]
        public const string KuduUseAppOfflineHtmFile = "Arbor.Build.Tools.External.Kudu.UseAppOfflineHtmFile";

        [VariableDescription("Flag to indicate if Kudu should exclude App_Data directory when deploying")]
        public const string KuduExcludeDeleteAppData = "Arbor.Build.Tools.External.Kudu.ExcludeDeleteApp_Data";

        [VariableDescription("Enable XUnit .NET Core App")]
        public const string XUnitNetCoreAppV2Enabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreAppV2.Enabled";

        [VariableDescription("Enable XUnit .NET Core App")]
        public const string XUnitNetCoreAppEnabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreApp.Enabled";

        [VariableDescription("Enable XUnit .NET Core App XML report XSLT V2 to Junit")]
        public const string XUnitNetCoreAppV2XmlXsltToJunitEnabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreApp.Xslt.V2ToJunit.Enabled";

        [VariableDescription("Enable XUnit .NET Core App XML report XSLT V2 to Junit")]
        public const string XUnitNetCoreAppV2TrxXsltToJunitEnabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreApp.Xslt.TrxToJunit.Enabled";

        [VariableDescription("Enable XUnit XML analysis for .NET Core App")]
        public const string XUnitNetCoreAppXmlAnalysisEnabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreApp.Xml.Analysis.Enabled";


        [VariableDescription("XUnit .NET Core App XML output enabled")]
        public const string XUnitNetCoreAppXmlEnabled =
            "Arbor.Build.Tools.External.Xunit.NetCoreApp.Xml.Enabled";

        [VariableDescription("'|' (bar) separated list of file names to not delete when deploying")]
        public const string KuduIgnoreDeleteFiles =
            "Arbor.Build.Tools.External.Kudu.IgnoreDeleteFilesBarSeparatedList";

        [VariableDescription("'|' (bar) separated list of directory names to not delete when deploying")]
        public const string KuduIgnoreDeleteDirectories =
            "Arbor.Build.Tools.External.Kudu.IgnoreDeleteDirectoriesBarSeparatedList";

        [VariableDescription(
            "Site for Kudu to deploy, needs to be specified if there are multiple web projects. Name of the csproj file exception the extension.")]
        public const string KuduSiteToDeploy = "Arbor.Build.Tools.External.Kudu.SiteToDeploy";

        [VariableDescription("Flag to indicate if Kudu should delete any existing app_offline.htm file when deploying")]
        public const string KuduDeleteExistingAppOfflineHtmFile =
            "Arbor.Build.Tools.External.Kudu.DeleteExistingAppOfflineHtmFile";

        [VariableDescription("Log level")]
        public const string LogLevel = "Arbor.Build.Log.Level";

        [VariableDescription("Generic XML transformaions enabled")]
        public const string GenericXmlTransformsEnabled = "Arbor.Build.Build.XmlTransformations.Enabled";

        [VariableDescription("Run tests in release configuration")]
        public const string RunTestsInReleaseConfigurationEnabled =
            "Arbor.Build.Tests.RunTestsInReleaseConfiguration";

        [VariableDescription("Run tests in any configuration")]
        public const string RunTestsInAnyConfigurationEnabled =
            "Arbor.Build.Tests.RunTestsInAnyConfiguration";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public const string CleanBinXmlFilesForAssembliesEnabled =
            "Arbor.Build.Build.WebApplications.CleanBinXmlFilesForAssembliesEnabled";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public const string CleanWebJobsXmlFilesForAssembliesEnabled =
            "Arbor.Build.Build.WebApplications.WebJobs.CleanWebJobsXmlFilesForAssembliesEnabled";

        [VariableDescription(
            "List of file name parts to be used when excluding files from being copied to web jobs directory, comma separated")]
        public const string WebJobsExcludedFileNameParts =
            "Arbor.Build.Build.WebApplications.WebJobs.ExcludedFileNameParts";

        [VariableDescription(
            "List of file name parts to be used when excluding directories from being copied to web jobs directory, comma separated")]
        public const string WebJobsExcludedDirectorySegments =
            "Arbor.Build.Build.WebApplications.WebJobs.ExcludedDirectorySegments";

        [VariableDescription(
            "List of file patterns to be used when excluding files to be included in a NuGet Web Package, comma separated")]
        public const string ExcludedNuGetWebPackageFiles =
            "Arbor.Build.NuGet.NuGetWebPackage.ExcludedPatterns";

        [VariableDescription("Cleanup known processes after build")]
        public const string CleanupProcessesAfterBuildEnabled =
            "Arbor.Build.Build.Cleanup.KillProcessesAfterBuild.Enabled";

        [VariableDescription(".NET Core publish runtime identifier")]
        public const string PublishRuntimeIdentifier =
            "Arbor.Build.Build.PublishRuntimeIdentifier";

        [VariableDescription("Paket Enabled")]
        public const string PaketEnabled =
            "Arbor.Build.Build.Paket.Enabled";

        [VariableDescription(".NET Core MSBuild web publish runtime identifiers")]
        public const string ProjectMSBuildPublishRuntimeIdentifier =
            "ArborBuild_PublishRuntimeIdentifier";

        [VariableDescription("Colon separated list of platforms to be excluded")]
        public const string MSBuildExcludedPlatforms = "Arbor.Build.Build.MSBuild.ExcludedPlatforms";

        public static IReadOnlyCollection<VariableDescription> AllVariables
        {
            get
            {
                var allVariables = new List<VariableDescription>();

                Type item = typeof(WellKnownVariables);
                var classes = new List<Type> { item };

                classes.AddRange(GetNestedClassTypes(item));

                ImmutableArray<FieldInfo> fields = classes
                    .Select(@class => @class
                        .GetFields()
                        .Where(field => field.IsPublicConstantOrStatic()))
                    .SelectMany(_ => _)
                    .ToImmutableArray();

                foreach (FieldInfo field in fields)
                {
                    string? invariantName = (string)field.GetValue(null);

                    if (string.IsNullOrWhiteSpace(invariantName))
                    {
                        continue;
                    }

                    var attribute = field.GetCustomAttribute<VariableDescriptionAttribute>();

                    VariableDescription description = attribute != null
                        ? VariableDescription.Create(
                            invariantName,
                            attribute.Description,
                            field.Name,
                            attribute.DefaultValue)
                        : VariableDescription.Create(field.Name);

                    allVariables.Add(description);
                }

                return allVariables.OrderBy(name => name.InvariantName).ToList();
            }
        }

        private static ImmutableArray<Type> GetNestedClassTypes(Type staticClass)
        {
            List<Type> nestedPublicStaticClasses = staticClass
                .GetNestedTypes(BindingFlags.Static | BindingFlags.Public)
                .Where(type => type.IsClass)
                .ToList();

            Type[] asArray = nestedPublicStaticClasses.ToArray();

            foreach (Type nestedPublicStaticClass in asArray)
            {
                nestedPublicStaticClasses.AddRange(GetNestedClassTypes(nestedPublicStaticClass));
            }

            return nestedPublicStaticClasses.ToImmutableArray();
        }

        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore InconsistentNaming
    }
}
