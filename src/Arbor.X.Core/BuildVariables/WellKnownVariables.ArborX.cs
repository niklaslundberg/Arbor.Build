namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescriptionAttribute("External tools path")]
        public static readonly string ExternalTools = Arbor.X.Build + ".Tools.External";

        [VariableDescriptionAttribute("Source root override")]
        public static readonly string SourceRootOverride = Arbor.X.Build + ".Source.Override";

        [VariableDescriptionAttribute("Test framework report path")]
        public static readonly string ReportPath = "Arbor.X.Artifacts.TestReports";
    }
}