namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if directory clone is enabled")]
        public static readonly string DirectoryCloneEnabled = "Arbor.X.Vcs.DirectoryCloneEnabled";

        [VariableDescription("Flag to indicate if directory clone is enabled", "false")]
        public static readonly string BranchNameVersionOverrideEnabled = "Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled";

        [VariableDescription("VCS branch full name")]
        public static readonly string BranchFullName = "Arbor.X.Vcs.Branch.FullName";

        [VariableDescription("VCS branch logical name")]
        public static readonly string BranchLogicalName = "Arbor.X.Vcs.Branch.LogicalName";

        [VariableDescription("VCS branch name version if any")]
        public static readonly string BranchNameVersion = "Arbor.X.Vcs.Branch.Name.Version";

        [VariableDescription("VCS branch name version split charecter comma separated list")]
        public static readonly string NameVersionCommanSeparatedSplitList = "Arbor.X.Vcs.Branch.NameVersionCommaSeparatedSplitList";
        
        [VariableDescription("Current branch name for the version control system")]
        public static readonly string BranchName = "Arbor.X.Vcs.Branch.Name";
    }

}
