using System.Collections.Generic;
using System.Reflection;

namespace Arbor.X.Core.BuildVariables
{
    public static class WellKnownVariables
    {
// ReSharper disable InconsistentNaming

// ReSharper disable ConvertToConstant.Global
        public static readonly string ExternalTools = "Arbor.X.Tools.External";

        public static readonly string ReportPath = "Arbor.X.Artifacts.TestReports";

        public static readonly string ExternalTools_VisualStudio_Version =
            "Arbor.X.Tools.External.VisualStudio.Version";

        public static readonly string Artifacts = "Arbor.X.Artifacts";

        public static readonly string Version = "Arbor.X.Build.Version";
        
        public static readonly string CpuLimit = "Arbor.X.CpuLimit";

        public static readonly string NetAssemblyVersion = "Arbor.X.Build.NetAssembly.Version";

        public static readonly string NetAssemblyFileVersion = "Arbor.X.Build.NetAssembly.FileVersion";

        public static readonly string ReleaseBuild = "Arbor.X.Build.IsReleaseBuild";

        public static readonly string Configuration = "Arbor.X.Build.Configuration";

        public static readonly string BranchName = "Arbor.X.Vcs.Branch.Name";

        public static readonly string TempDirectory = "Arbor.X.Build.TempDirectory";

        public static readonly string ExternalTools_NuGet_ExePath = "Arbor.X.Tools.External.NuGet.ExePath";

        public static readonly string ExternalTools_SymbolServer_Uri = "Arbor.X.Tools.External.SymbolServer.Uri";

        public static readonly string ExternalTools_SymbolServer_ApiKey = "Arbor.X.Tools.External.SymbolServer.ApiKey";

        public static readonly string IsRunningOnBuildAgent = "Arbor.X.Build.IsRunningOnBuildAgent";

        public static readonly string AllowPrerelease = "Arbor.X.Build.Bootstrapper.AllowPrerelease";

        public static readonly string ExternalTools_MSBuild_ExePath = "Arbor.X.Tools.External.MSBuild.ExePath";

        public static readonly string SourceRoot = "SourceRoot";

        public static readonly string IgnoreAnyCpu = "Arbor.X.Build.Platform.AnyCPU.Disabled";

        public static readonly string IgnoreRelease = "Arbor.X.Build.Configuration.Release.Disabled";

        public static readonly string IgnoreDebug = "Arbor.X.Build.Configuration.Debug.Disabled";

        public static readonly string IgnoreTestFailures = "Arbor.X.Build.Tests.IgnoreTestFailures";

        public static readonly string ExternalTools_VSTest_ExePath = "Arbor.X.Tools.External.VSTest.ExePath";

        public static readonly string ExternalTools_VSTest_ReportPath = "Arbor.X.Artifacts.TestReports.VSTest";

        public static readonly string ExternalTools_ILMerge_ExePath = "Arbor.X.Tools.External.ILMerge.ExePath";

        public static readonly string ExternalTools_Kudu_Enabled = "Arbor.X.Tools.External.Kudu.Enabled";

        public static readonly string ExternalTools_Kudu_DeploymentTarget = "DEPLOYMENT_TARGET";

        public static readonly string ExternalTools_Kudu_Platform = "REMOTEDEBUGGINGBITVERSION";

        public static readonly string ExternalTools_Kudu_DeploymentBranchName = "deployment_branch";

        public static readonly string ExternalTools_Kudu_DeploymentBranchNameOverride = "Arbor.X.Tools.External.Kudu.DeploymentBranchNameOverride";

        public static readonly string ExternalTools_Kudu_ProcessorCount = "NUMBER_OF_PROCESSORS";

        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore InconsistentNaming

        public static IEnumerable<KeyValuePair<string, string>> Keys
        {
            get
            {
                var fields = typeof (WellKnownVariables).GetFields(BindingFlags.Public | BindingFlags.Static);

                foreach (var field in fields)
                {
                    var value = (string) field.GetValue(null);
                    yield return new KeyValuePair<string, string>(field.Name, value);
                }
            }
        }
    }
}