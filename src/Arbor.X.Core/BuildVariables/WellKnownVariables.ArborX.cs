namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("External tools path")]
        public static readonly string ExternalTools = Arbor.X.Build + ".Tools.External";

        [VariableDescription("Source root override")]
        public static readonly string SourceRootOverride = Arbor.X.Build + ".Source.Override";

        [VariableDescription("Test framework report path")]
        public static readonly string ReportPath = "Arbor.X.Artifacts.TestReports";

        [VariableDescription("Show available variables")]
        public static readonly string ShowAvailableVariablesEnabled = "Arbor.X.ShowAvailableVariablesEnabled";

        [VariableDescription("Show defined variables")]
        public static readonly string ShowDefinedVariablesEnabled = "Arbor.X.ShowDefinedVariablesEnabled";

        [VariableDescription("Flag to indicate if applicationmetadata.json should be created dynamically", "false")]
        public static readonly string ApplicationMetadataEnabled = "Arbor.X.ApplicationMetadata.Enabled";
    }
}
