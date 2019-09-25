using System;
using System.Linq;
using Arbor.Defensive;

namespace Arbor.Build.Core.Tools.Git
{
    public sealed class BranchName
    {
        public BranchName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public string LogicalName => BranchHelper.GetLogicalName(Name).Name;

        public string FullName => Name;

        public static Maybe<BranchName> TryParse(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return Maybe<BranchName>.Empty();
            }

            return new Maybe<BranchName>(new BranchName(branchName));
        }

        public override string ToString() => Name;

        public string Normalize()
        {
            var invalidCharacters = new[] { "/", @"\", "\"" };

            string branchNameWithValidCharacters = invalidCharacters.Aggregate(
                Name,
                (current, invalidCharacter) =>
                    current.Replace(invalidCharacter, "-"));

            string removedFeatureInName = branchNameWithValidCharacters.Replace("feature-", string.Empty);

            return removedFeatureInName;
        }
    }
}