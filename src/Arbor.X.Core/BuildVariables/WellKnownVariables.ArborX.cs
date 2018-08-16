namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if web projects should be built")]
        public const string WebProjectsBuildEnabled = "Arbor.X.Build.WebProjectsBuild.Enabled";

        [VariableDescription("Use assembly reflection only mode enabled")]
        public const string AssemblyUseReflectionOnlyMode = "Arbor.X.ReflectionAssemblyLoad.Enabled";

        [VariableDescription("External tools path")]
        public const string ExternalTools =
            "Arbor.X.Build.Tools.External";

        [VariableDescription("Source root override")]
        public const string SourceRootOverride =
            "Arbor.X.Build.Source.Override";

        [VariableDescription("Test framework report path")]
        public const string ReportPath =
            "Arbor.X.Artifacts.TestReports";

        [VariableDescription("Show available variables")]
        public const string ShowAvailableVariablesEnabled =
            "Arbor.X.ShowAvailableVariablesEnabled";

        [VariableDescription("Show defined variables")]
        public const string ShowDefinedVariablesEnabled =
            "Arbor.X.ShowDefinedVariablesEnabled";

        [VariableDescription("Flag to indicate if applicationmetadata.json should be created dynamically", "false")]
        public const string ApplicationMetadataEnabled = "Arbor.X.ApplicationMetadata.Enabled";

        [VariableDescription(
            "Flag to indicate if Git hash should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataGitHashEnabled = "Arbor.X.ApplicationMetadata.GitHash.Enabled";

        [VariableDescription(
            "Flag to indicate if Git branch name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataGitBranchEnabled =
            "Arbor.X.ApplicationMetadata.GitBranch.Enabled";

        [VariableDescription(
            "Flag to indicate if .NET CPU platform name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataDotNetCpuPlatformEnabled =
            "Arbor.X.ApplicationMetadata.DotNetCpuPlatform.Enabled";

        [VariableDescription(
            "Flag to indicate if .NET build configuration name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataDotNetConfigurationEnabled =
            "Arbor.X.ApplicationMetadata.DotNetConfiguration.Enabled";
    }
}
