using System;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class GitModel : IEquatable<GitModel>
    {
        public static readonly GitModel GitFlowBuildOnMaster = new GitModel(nameof(GitFlowBuildOnMaster));

        public GitModel(string name) => Name = name;

        public string Name { get; }

        public bool Equals(GitModel? other)
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

        public static bool TryParse(string? value, out GitModel? model)
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
            ReferenceEquals(this, obj) || (obj is GitModel other && Equals(other));

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(GitModel? left, GitModel? right) => Equals(left, right);

        public static bool operator !=(GitModel? left, GitModel? right) => !Equals(left, right);
    }
}