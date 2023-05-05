using System;
using System.Linq;

namespace Arbor.Build.Core.Tools.Git;

public sealed class BranchName
{
    public static readonly BranchName Main = new("main");
    public static readonly BranchName Master = new("master");
    public static readonly BranchName Develop = new("develop");
    private static readonly string[] InvalidCharacters = { "/", @"\", "\"" };

    public BranchName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
    }

    public bool IsMainBranch =>
        Equals(Main) || Equals(Master) ||
        LogicalName.Equals(Master.LogicalName, StringComparison.Ordinal) ||
        LogicalName.Equals(Main.LogicalName, StringComparison.Ordinal);

    public string Name { get; }

    public string LogicalName => BranchHelper.GetLogicalName(Name).Name;

    public string FullName => Name;

    public static BranchName? TryParse(string? branchName) => string.IsNullOrWhiteSpace(branchName) ? default : new BranchName(branchName);

    public override string ToString() => Name;

    public string Normalize()
    {
        string branchNameWithValidCharacters = InvalidCharacters.Aggregate(
            LogicalName,
            (current, invalidCharacter) =>
                current.Replace(invalidCharacter, "-", StringComparison.Ordinal));

        return branchNameWithValidCharacters.Replace("feature-", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}