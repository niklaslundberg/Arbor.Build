using System;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class GitBranchModel : IEquatable<GitBranchModel>
    {
        public static readonly GitBranchModel GitFlowBuildOnMain =
            new GitBranchModel(nameof(GitFlowBuildOnMain), "GitFlowBuildOnMaster");

        public GitBranchModel(string name, string? alias = null)
        {
            Name = name;
            Alias = alias;
        }

        public string Name { get; }

        public string? Alias { get; }

        public bool Equals(GitBranchModel? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name;
        }

        public static bool TryParse(string? value, out GitBranchModel? model)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                model = default;
                return false;
            }

            if (value.Equals(GitFlowBuildOnMain.Name, StringComparison.OrdinalIgnoreCase))
            {
                model = GitFlowBuildOnMain;
                return true;
            }

            if (value.Equals(GitFlowBuildOnMain.Alias, StringComparison.OrdinalIgnoreCase))
            {
                model = GitFlowBuildOnMain;
                return true;
            }

            model = default;
            return false;
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || (obj is GitBranchModel other && Equals(other));

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(GitBranchModel? left, GitBranchModel? right) => Equals(left, right);

        public static bool operator !=(GitBranchModel? left, GitBranchModel? right) => !Equals(left, right);
    }
}