using System;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class GitBranchModel : IEquatable<GitBranchModel>
    {
        public static readonly GitBranchModel GitFlowBuildOnMaster = new GitBranchModel(nameof(GitFlowBuildOnMaster));

        public GitBranchModel(string name) => Name = name;

        public string Name { get; }

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

            if (value.Equals(GitFlowBuildOnMaster.Name, StringComparison.OrdinalIgnoreCase))
            {
                model = GitFlowBuildOnMaster;
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