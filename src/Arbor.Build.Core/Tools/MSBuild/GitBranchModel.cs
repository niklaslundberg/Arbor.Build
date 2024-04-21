using System;

namespace Arbor.Build.Core.Tools.MSBuild;

public sealed class GitBranchModel(string name, string? alias = null) : IEquatable<GitBranchModel>
{
    public static readonly GitBranchModel GitFlowBuildOnMain =
        new(nameof(GitFlowBuildOnMain), "GitFlowBuildOnMaster");

    public string Name { get; } = name;

    public string? Alias { get; } = alias;

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

    public override bool Equals(object? obj) => Equals(obj as GitBranchModel);

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


    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(GitBranchModel? left, GitBranchModel? right) => Equals(left, right);

    public static bool operator !=(GitBranchModel? left, GitBranchModel? right) => !Equals(left, right);
}