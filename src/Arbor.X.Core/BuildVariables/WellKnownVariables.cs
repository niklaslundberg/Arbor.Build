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
        public const string DotNetPublishExeProjectsEnabled = "Arbor.X.Build.PublishDotNetExecutableProjects";

        [VariableDescription("Flag to enabled dotnet pack for tool netcoreapp projects")]
        public const string DotNetPackToolProjectsEnabled = "Arbor.X.Build.PackDotNetToolProjects";

        [VariableDescription("Flag to indicate if build platform AnyCPU is enabled")]
        public const string AnyCpuEnabled = "Arbor.X.Build.Platform.AnyCPU.Enabled";

        [VariableDescription(
            "Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public const string ExternalTools_SymbolServer_UploadTimeoutInSeconds =
            "Arbor.X.NuGet.SymbolServer.TimeoutInSeconds";

        [VariableDescription("External, Kudu: deployment version control branch")]
        public const string ExternalTools_Kudu_DeploymentBranchName = "deployment_branch";

        public const string DotNetRestoreEnabled = "Arbor.X.DotNet.Restore.Enabled";

        public const string MSBuildNuGetRestoreEnabled = "Arbor.X.MSBuild.NuGetRestore.Enabled";

        public const string DotNetExePath = "Arbor.X.DotNet.ExePath";

        [VariableDescription("Visual Studio version")]
        public const string ExternalTools_VisualStudio_Version =
            "Arbor.X.Tools.External.VisualStudio.Version";

        [VariableDescription("Visual Studio version")]
        public const string ExternalTools_VisualStudio_Version_Allow_PreRelease =
            "Arbor.X.Tools.External.VisualStudio.Version.PreRelease.Enabled";

        [VariableDescription("Build arftifacts path")]
        public const string Artifacts = "Arbor.X.Artifacts";

        [VariableDescription("Flag to indicate if the build arftifacts should be cleaned up before the build starts")]
        public const string CleanupArtifactsBeforeBuildEnabled =
            "Arbor.X.Artifacts.CleanupBeforeBuildEnabled";

        [VariableDescription("Full build version number")]
        public const string Version =
            "Arbor.X.Build.Version";

        [VariableDescription("Max number of CPUs for MSBuild to use")]
        public const string CpuLimit =
            "Arbor.X.CpuLimit";

        [VariableDescription(".NET assembly version")]
        public const string NetAssemblyVersion =
            "Arbor.X.Build.NetAssembly.Version";

        [VariableDescription(".NET assembly file version")]
        public const string NetAssemblyFileVersion =
            "Arbor.X.Build.NetAssembly.FileVersion";

        [VariableDescription("Enable assembly version patching")]
        public const string AssemblyFilePatchingEnabled = "Arbor.X.Build.NetAssembly.PatchingEnabled";

        [VariableDescription("Flag to indicate if the build is consider a release build")]
        public const string ReleaseBuild = "Arbor.X.Build.IsReleaseBuild";

        [VariableDescription("MSBuild configuration (eg. Debug/Release)")]
        public const string Configuration =
            "Arbor.X.Build.Configuration";

        [VariableDescription("Dynamic configuration property")]
        public const string CurrentBuildConfiguration = "Arbor.X.Build.CurrentBuild.Configuration";

        [VariableDescription("Temporary directory path")]
        public const string TempDirectory =
            "Arbor.X.Build.TempDirectory";

        [VariableDescription("Symbol server URI for NuGet source package upload")]
        public const string ExternalTools_SymbolServer_Uri = "Arbor.X.Tools.External.SymbolServer.Uri";

        [VariableDescription("Symbol server API key for NuGet source package upload")]
        public const string ExternalTools_SymbolServer_ApiKey = "Arbor.X.Tools.External.SymbolServer.ApiKey";

        [VariableDescription(
            "Flag to indicate that Symbol server package upload is enabled even if not running on a build server")]
        public const string ExternalTools_SymbolServer_ForceUploadEnabled =
            "Arbor.X.Tools.External.SymbolServer.ForceUploadEnabled";

        [VariableDescription("Flag to indicate that Symbol server package upload is enabled")]
        public const string ExternalTools_SymbolServer_Enabled =
            "Arbor.X.Tools.External.SymbolServer.Enabled";

        [VariableDescription("Flag to indicate if the build is running on a build agent")]
        public const string IsRunningOnBuildAgent = "Arbor.X.Build.IsRunningOnBuildAgent";

        [VariableDescription(
            "Flag to indicate if the bootstrapper is allowed to download pre-release versions of Arbor.X NuGet package",
            "false")]
        public const string AllowPrerelease = "Arbor.X.Build.Bootstrapper.AllowPrerelease";

        [VariableDescription("Arbor.X NuGet package version for bootstrapper to download")]
        public const string ArborBuildNuGetPackageVersion = "Arbor.Build.NuGetPackageVersion";

        [VariableDescription("NuGet source to use when downloading Arbor.X NuGet package")]
        public const string ArborXNuGetPackageSource = "Arbor.X.NuGetPackage.Source";

        [VariableDescription(
            "Flag to indicate if the bootstrapper should use -NoCache flag when downloading Arbor.X NuGet package")]
        public const string ArborXNuGetPackageNoCacheEnabled = "Arbor.X.NuGetPackage.NoCachedEnabled";

        [VariableDescription("MSBuild executable path (eg. C:\\MSbuild.exe)")]
        public const string ExternalTools_MSBuild_ExePath = "Arbor.X.Tools.External.MSBuild.ExePath";

        [VariableDescription("MSBuild max version")]
        public const string ExternalTools_MSBuild_MaxVersion =
            "Arbor.X.Tools.External.MSBuild.MaxVersion";

        [VariableDescription("MSBuild max version")]
        public const string ExternalTools_MSBuild_AllowPrereleaseEnabled =
            "Arbor.X.Tools.External.MSBuild.AllowPrerelease.Enabled";

        [VariableDescription("MSBuild verbosity level", "normal")]
        public const string ExternalTools_MSBuild_Verbosity = "Arbor.X.Tools.External.MSBuild.Verbosity";

        [VariableDescription("Flag to indicate if MSBuild should display a build summary", "false")]
        public const string ExternalTools_MSBuild_SummaryEnabled =
            "Arbor.X.Tools.External.MSBuild.SummaryEnabled";

        [VariableDescription(
            "MSBuild build configuration, if not specified, all wellknown configurations will be built")]
        public const string ExternalTools_MSBuild_BuildConfiguration =
            "Arbor.X.Tools.External.MSBuild.BuildConfiguration";

        [VariableDescription("MSBuild build platform, if not specified, all wellknown platforms will be built")]
        public const string ExternalTools_MSBuild_BuildPlatform =
            "Arbor.X.Tools.External.MSBuild.BuildPlatform";

        [VariableDescription("Flag to indicate if code analysis should be run by MSBuild")]
        public const string ExternalTools_MSBuild_CodeAnalysisEnabled =
            "Arbor.X.Tools.External.MSBuild.CodeAnalysis.Enabled";

        [VariableDescription("MSBuild detault target when building")]
        public const string ExternalTools_MSBuild_DefaultTarget =
            "Arbor.X.Tools.External.MSBuild.DefaultTarget";

        [VariableDescription("Directory path for the current version control system repository")]
        public const string SourceRoot = "SourceRoot";

        [VariableDescription("Flag to indicate if build platform AnyCPU is disabled", "false", AnyCpuEnabled)]
        public const string IgnoreAnyCpu = "Arbor.X.Build.Platform.AnyCPU.Disabled";

        [VariableDescription("Flag to indicate if build configuration Release is enabled", "true")]
        public const string ReleaseBuildEnabled = "Arbor.X.Build.Configuration.Release.Enabled";

        [VariableDescription("Flag to indicate if build platform configuration Debug is enabled", "true")]
        public const string DebugBuildEnabled = "Arbor.X.Build.Configuration.Debug.Enabled";

        [VariableDescription("Flag to indicate if test runner error results are ignored", "false")]
        public const string IgnoreTestFailures = "Arbor.X.Build.Tests.IgnoreTestFailures";

        [VariableDescription(
            "Comma separated list to filter assemblies, to only run tests dlls starting with prefix, case insensitive",
            "")]
        public const string TestsAssemblyStartsWith = "Arbor.X.Build.Tests.AssemblyStartsWith";

        [VariableDescription("Test categories and tags to ignore, comma separated")]
        public const string IgnoredTestCategories = "Arbor.X.Build.Tests.IgnoredCategories";

        [VariableDescription("Flag to indicate if tests are enabled", "false")]
        public const string TestsEnabled = "Arbor.X.Build.Tests.Enabled";

        [VariableDescription(
            "Visual Studio Test Framework console application path, (eg. C:\\VSTestConsole.exe)",
            "false")]
        public const string ExternalTools_VSTest_ExePath =
            "Arbor.X.Tools.External.VSTest.ExePath";

        [VariableDescription("Visual Studio Test Framework test reports directory path")]
        public const string ExternalTools_VSTest_ReportPath = "Arbor.X.Artifacts.TestReports.VSTest";

        [VariableDescription("Machine.Specifications reports directory path")]
        public const string ExternalTools_MSpec_ReportPath = "Arbor.X.Artifacts.TestReports.MSpec";

        [VariableDescription("PDB artifacts enabled")]
        public const string PublishPdbFilesAsArtifacts =
            "Arbor.X.Artifacts.PdbArtifacts.Enabled";

        [VariableDescription("ILRepack executable path (eg. C:\\ILRepack.exe)")]
        public const string ExternalTools_ILRepack_ExePath = "Arbor.X.Tools.External.ILRepack.ExePath";

        [VariableDescription("ILRepack enabled (eg. true|false")]
        public const string ExternalTools_ILRepack_Enabled = "Arbor.X.Tools.External.ILRepack.Enabled";

        [VariableDescription("ILRepack custom executable path (eg. C:\\ILRepack.exe)")]
        public const string ExternalTools_ILRepack_Custom_ExePath =
            "Arbor.X.Tools.External.ILRepack.CustomExePath";

        [VariableDescription("LibZ executable path (eg. C:\\LibZ.exe)")]
        public const string ExternalTools_LibZ_ExePath = "Arbor.X.Tools.External.LibZ.ExePath";

        [VariableDescription("LibZ custom executable path (eg. C:\\LibZ.exe)")]
        public const string ExternalTools_LibZ_Custom_ExePath = "Arbor.X.Tools.External.LibZ.CustomExePath";

        [VariableDescription("LibZ enabled (eg. true|false")]
        public const string ExternalTools_LibZ_Enabled =
            "Arbor.X.Tools.External.LibZ.Enabled";

        [VariableDescription("Flag to indicate if Kudu deployment is enabled", "true")]
        public const string ExternalTools_Kudu_Enabled = "Arbor.X.Tools.External.Kudu.Enabled";

        [VariableDescription("External, Kudu: deployment target directory path (website public directory)")]
        public const string ExternalTools_Kudu_DeploymentTarget = "DEPLOYMENT_TARGET";

        [VariableDescription("External, Kudu: site running as x86 or x64 process")]
        public const string ExternalTools_Kudu_Platform = "REMOTEDEBUGGINGBITVERSION";

        [VariableDescription("Deployment branch to be used in Kudu, overrides value defined in " +
                             ExternalTools_Kudu_DeploymentBranchName)]
        public const string ExternalTools_Kudu_DeploymentBranchNameOverride =
            "Arbor.X.Tools.External.Kudu.DeploymentBranchNameOverride";

        [VariableDescription("External, Kudu: number of processors available on the current system")]
        public const string ExternalTools_Kudu_ProcessorCount = "NUMBER_OF_PROCESSORS";

        [VariableDescription(
            "Flag to indicate if Kudu WebJobs defined in App_Data directory is to be handled by the Kudu WebJobs aware tools")]
        public const string AppDataJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.AppData.Enabled";

        [VariableDescription(
            "MSBuild configuration to be used to locate web application artifacts to be deployed, if not found by the tools")]
        public const string KuduConfigurationFallback = "Arbor.X.Tools.External.Kudu.ConfigurationFallback";

        [VariableDescription("Flag to indicate if Kudu WebJobs is to be handles by the Kudu WebJobs aware tools")]
        public const string KuduJobsEnabled = "Arbor.X.Tools.External.Kudu.WebJobs.Enabled";

        [VariableDescription("Time out in seconds for total build process")]
        public const string BuildToolTimeoutInSeconds = "Arbor.X.Build.TimeoutInSeconds";

        [VariableDescription("Bootstrapper exit delay in milliseconds")]
        public const string BootstrapperExitDelayInMilliseconds =
            "Arbor.X.Bootstrapper.ExitDelayInMilliseconds";

        [VariableDescription("Build application exit delay in milliseconds")]
        public const string BuildApplicationExitDelayInMilliseconds =
            "Arbor.X.Build.ExitDelayInMilliseconds";

        [VariableDescription("Flag to indicate if defined variables can be overriden")]
        public const string VariableOverrideEnabled = "Arbor.X.Build.VariableOverrideEnabled";

        [VariableDescription(
            "Flag to indicate if a file arborx_environmentvariables.json should be used as a source to set environment variables")]
        public const string VariableFileSourceEnabled = "Arbor.X.Build.VariableFileSource.Enabled";

        [VariableDescription(
            "Flag to indicate if Kudu target path files and directories should be deleted before deploy")]
        public const string KuduClearFilesAndDirectories = "Arbor.X.Tools.External.Kudu.ClearEnabled";

        [VariableDescription("Flag to indicate if Kudu should use app_offline.htm file when deploying")]
        public const string KuduUseAppOfflineHtmFile = "Arbor.X.Tools.External.Kudu.UseAppOfflineHtmFile";

        [VariableDescription("Flag to indicate if Kudu should exclude App_Data directory when deploying")]
        public const string KuduExcludeDeleteAppData = "Arbor.X.Tools.External.Kudu.ExcludeDeleteApp_Data";

        [VariableDescription("Enable Machine.Specifications")]
        public const string MSpecEnabled =
            "Arbor.X.Tools.External.MSpec.Enabled";

        [VariableDescription("Enable Machine.Specifications XSL transformation to NUnit")]
        public const string MSpecJUnitXslTransformationEnabled =
            "Arbor.X.Tools.External.MSpec.JUnitXslTransformation.Enabled";

        [VariableDescription("Enable NUnit")]
        public const string NUnitEnabled =
            "Arbor.X.Tools.External.NUnit.Enabled";

        [VariableDescription("NUnitExePathOverride")]
        public const string NUnitExePathOverride =
            "Arbor.X.Tools.External.NUnit.ExePathOverride";

        [VariableDescription("NUnit JUnit XSL transform enabled")]
        public const string NUnitTransformToJunitEnabled =
            "Arbor.X.Tools.External.NUnit.JUnitXslTransform.Enabled";

        [VariableDescription("Enable XUnit .NET Framework")]
        public const string XUnitNetFrameworkEnabled =
            "Arbor.X.Tools.External.Xunit.NetFramework.Enabled";

        [VariableDescription("Enable XUnit .NET Core App")]
        public const string XUnitNetCoreAppV2Enabled =
            "Arbor.X.Tools.External.Xunit.NetCoreAppV2.Enabled";

        [VariableDescription("Enable XUnit .NET Core App")]
        public const string XUnitNetCoreAppEnabled =
            "Arbor.X.Tools.External.Xunit.NetCoreApp.Enabled";

        [VariableDescription("Enable XUnit .NET Core App XML report XSLT V2 to Junit")]
        public const string XUnitNetCoreAppV2XmlXsltToJunitEnabled =
            "Arbor.X.Tools.External.Xunit.NetCoreApp.Xslt.V2ToJunit.Enabled";

        [VariableDescription("Enable XUnit XML analysis for .NET Core App")]
        public const string XUnitNetCoreAppXmlAnalysisEnabled =
            "Arbor.X.Tools.External.Xunit.NetCoreApp.Xml.Analysis.Enabled";

        [VariableDescription("XUnit .NET Core App DLL path")]
        public const string XUnitNetCoreAppDllPath =
            "Arbor.X.Tools.External.Xunit.NetCoreApp.DllPath";

        [VariableDescription("XUnit .NET Core App XML output enabled")]
        public const string XUnitNetCoreAppXmlEnabled =
            "Arbor.X.Tools.External.Xunit.NetCoreApp.Xml.Enabled";

        [VariableDescription("XUnit .NET Framework exe path")]
        public const string XUnitNetFrameworkExePath =
            "Arbor.X.Tools.External.Xunit.NetFramework.ExePath";

        [VariableDescription("Enable VSTest")]
        public const string VSTestEnabled =
            "Arbor.X.Tools.External.VSTest.Enabled";

        [VariableDescription("'|' (bar) separated list of file names to not delete when deploying")]
        public const string KuduIgnoreDeleteFiles =
            "Arbor.X.Tools.External.Kudu.IgnoreDeleteFilesBarSeparatedList";

        [VariableDescription("'|' (bar) separated list of directory names to not delete when deploying")]
        public const string KuduIgnoreDeleteDirectories =
            "Arbor.X.Tools.External.Kudu.IgnoreDeleteDirectoriesBarSeparatedList";

        [VariableDescription(
            "Site for Kudu to deploy, needs to be specified if there are multiple web projects. Name of the csproj file exception the extension.")]
        public const string KuduSiteToDeploy = "Arbor.X.Tools.External.Kudu.SiteToDeploy";

        [VariableDescription("Flag to indicate if Kudu should delete any existing app_offline.htm file when deploying")]
        public const string KuduDeleteExistingAppOfflineHtmFile =
            "Arbor.X.Tools.External.Kudu.DeleteExistingAppOfflineHtmFile";

        [VariableDescription("Log level")]
        public const string LogLevel = "Arbor.X.Log.Level";

        [VariableDescription("Generic XML transformaions enabled")]
        public const string GenericXmlTransformsEnabled = "Arbor.X.Build.XmlTransformations.Enabled";

        [VariableDescription("Run tests in release configuration")]
        public const string RunTestsInReleaseConfigurationEnabled =
            "Arbor.X.Tests.RunTestsInReleaseConfiguration";

        [VariableDescription("Run tests in any configuration")]
        public const string RunTestsInAnyConfigurationEnabled =
            "Arbor.X.Tests.RunTestsInAnyConfiguration";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public const string CleanBinXmlFilesForAssembliesEnabled =
            "Arbor.X.Build.WebApplications.CleanBinXmlFilesForAssembliesEnabled";

        [VariableDescription("Flag to indicate if XML files for assemblies in the bin directory should be deleted")]
        public const string CleanWebJobsXmlFilesForAssembliesEnabled =
            "Arbor.X.Build.WebApplications.WebJobs.CleanWebJobsXmlFilesForAssembliesEnabled";

        [VariableDescription(
            "List of file name parts to be used when excluding files from being copied to web jobs directory, comma separated")]
        public const string WebJobsExcludedFileNameParts =
            "Arbor.X.Build.WebApplications.WebJobs.ExcludedFileNameParts";

        [VariableDescription(
            "List of file name parts to be used when excluding directories from being copied to web jobs directory, comma separated")]
        public const string WebJobsExcludedDirectorySegments =
            "Arbor.X.Build.WebApplications.WebJobs.ExcludedDirectorySegments";

        [VariableDescription(
            "List of file patterns to be used when excluding files to be included in a NuGet Web Package, comma separated")]
        public const string ExcludedNuGetWebPackageFiles =
            "Arbor.X.NuGet.NuGetWebPackage.ExcludedPatterns";

        [VariableDescription("Cleanup known processes after build")]
        public const string CleanupProcessesAfterBuildEnabled =
            "Arbor.X.Build.Cleanup.KillProcessesAfterBuild.Enabled";

        [VariableDescription("Colon separated list of platforms to be excluded")]
        public const string MSBuildExcludedPlatforms = "Arbor.X.Build.MSBuild.ExcludedPlatforms";

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
                    string invariantName = (string)field.GetValue(null);

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
