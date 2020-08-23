﻿namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if directory clone is enabled")]
        public const string DirectoryCloneEnabled = "Arbor.Build.Vcs.DirectoryCloneEnabled";

        [VariableDescription("Flag to indicate if directory clone is enabled", "false")]
        public const string BranchNameVersionOverrideEnabled =
            "Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled";

        [VariableDescription("VCS branch full name")]
        public const string BranchFullName =
            "Arbor.Build.Vcs.Branch.FullName";

        [VariableDescription("VCS branch logical name")]
        public const string BranchLogicalName =
            "Arbor.Build.Vcs.Branch.LogicalName";

        [VariableDescription("Git hash")]
        public const string GitHash = "Arbor.Build.Vcs.Git.Hash";

        [VariableDescription("GitHub commit hash")]
        public const string GitHubSha = "GITHUB_SHA";

        [VariableDescription("Git repository url")]
        public const string RepositoryUrl = "Arbor.Build.Vcs.Git.RepositoryUrl";

        [VariableDescription("VCS branch name version if any")]
        public const string BranchNameVersion =
            "Arbor.Build.Vcs.Branch.Name.Version";

        [VariableDescription("VCS branch name version split character comma separated list")]
        public const string NameVersionCommonSeparatedSplitList =
            "Arbor.Build.Vcs.Branch.NameVersionCommaSeparatedSplitList";

        [VariableDescription("Current branch name for the version control system")]
        public const string BranchName = "Arbor.Build.Vcs.Branch.Name";

        [VariableDescription("Current GitHub branch name for the version control system")]
        public const string GitHubBranchName = "GITHUB_REF";

        [VariableDescription("Branching model used, used for default values")]
        public const string GitBranchModel = "Arbor.Build.Vcs.Branch.BranchModel";
    }
}
