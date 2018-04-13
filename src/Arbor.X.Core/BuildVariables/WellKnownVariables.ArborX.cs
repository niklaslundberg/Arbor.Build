namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if web projects should be built")]
        public const string WebProjectsBuildEnabled = "Arbor.X.Build.WebProjectsBuild.Enabled";

        [VariableDescription("External tools path")]
        public static readonly string ExternalTools =
            Arbor.X.Build + ".Tools.External";

        [VariableDescription("Source root override")]
        public static readonly string SourceRootOverride =
            Arbor.X.Build + ".Source.Override";

        [VariableDescription("Test framework report path")]
        public static readonly string ReportPath =
            "Arbor.X.Artifacts.TestReports";

        [VariableDescription("Show available variables")]
        public static readonly string ShowAvailableVariablesEnabled =
            "Arbor.X.ShowAvailableVariablesEnabled";

        [VariableDescription("Show defined variables")]
        public static readonly string ShowDefinedVariablesEnabled =
            "Arbor.X.ShowDefinedVariablesEnabled";

        [VariableDescription("Flag to indicate if applicationmetadata.json should be created dynamically", "false")]
        public static readonly string ApplicationMetadataEnabled = "Arbor.X.ApplicationMetadata.Enabled";

        [VariableDescription("Flag to indicate if Git hash should be added to applicationmetadata.json when it is created", "false")]
        public static readonly string ApplicationMetadataGitHashEnabled = "Arbor.X.ApplicationMetadata.GitHash.Enabled";

        [VariableDescription("Flag to indicate if Git branch name should be added to applicationmetadata.json when it is created", "false")]
        public static readonly string ApplicationMetadataGitBranchEnabled = "Arbor.X.ApplicationMetadata.GitBranch.Enabled";

        [VariableDescription("Flag to indicate if .NET CPU platform name should be added to applicationmetadata.json when it is created", "false")]
        public static readonly string ApplicationMetadataDotNetCpuPlatformEnabled = "Arbor.X.ApplicationMetadata.DotNetCpuPlatform.Enabled";

        [VariableDescription("Flag to indicate if .NET build configuration name should be added to applicationmetadata.json when it is created", "false")]
        public static readonly string ApplicationMetadataDotNetConfigurationEnabled = "Arbor.X.ApplicationMetadata.DotNetConfiguration.Enabled";

        [VariableDescription("Use assembly reflection only mode enabled")]
        public const string AssemblyUseReflectionOnlyMode = "Arbor.X.ReflectionAssemblyLoad.Enabled";
    }
}
