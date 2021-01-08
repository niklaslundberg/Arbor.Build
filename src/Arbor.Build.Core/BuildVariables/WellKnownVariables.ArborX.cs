namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if web projects should be built")]
        public const string WebProjectsBuildEnabled = "Arbor.Build.WebProjectsBuild.Enabled";

        [VariableDescription("Use assembly reflection only mode enabled")]
        public const string AssemblyUseReflectionOnlyMode = "Arbor.Build.ReflectionAssemblyLoad.Enabled";

        [VariableDescription("External tools path")]
        public const string ExternalTools =
            "Arbor.Build.Tools.External";

        [VariableDescription("Source root override")]
        public const string SourceRootOverride =
            "Arbor.Build.Source.Override";

        [VariableDescription("Test framework report path")]
        public const string ReportPath =
            "Arbor.Build.Artifacts.TestReports";

        [VariableDescription("Show available variables")]
        public const string ShowAvailableVariablesEnabled =
            "Arbor.Build.ShowAvailableVariablesEnabled";

        [VariableDescription("Show defined variables")]
        public const string ShowDefinedVariablesEnabled =
            "Arbor.Build.ShowDefinedVariablesEnabled";

        [VariableDescription("Flag to indicate if applicationmetadata.json should be created dynamically", "false")]
        public const string ApplicationMetadataEnabled = "Arbor.Build.ApplicationMetadata.Enabled";

        [VariableDescription(
            "Flag to indicate if Git hash should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataGitHashEnabled = "Arbor.Build.ApplicationMetadata.GitHash.Enabled";

        [VariableDescription(
            "Flag to indicate if Git branch name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataGitBranchEnabled =
            "Arbor.Build.ApplicationMetadata.GitBranch.Enabled";

        [VariableDescription(
            "Flag to indicate if .NET CPU platform name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataDotNetCpuPlatformEnabled =
            "Arbor.Build.ApplicationMetadata.DotNetCpuPlatform.Enabled";

        [VariableDescription(
            "Flag to indicate if .NET build configuration name should be added to applicationmetadata.json when it is created",
            "false")]
        public const string ApplicationMetadataDotNetConfigurationEnabled =
            "Arbor.Build.ApplicationMetadata.DotNetConfiguration.Enabled";
    }
}
