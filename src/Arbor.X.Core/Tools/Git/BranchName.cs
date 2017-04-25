using System;
using System.Linq;
using Arbor.Defensive;

namespace Arbor.X.Core.Tools.Git
{
    public sealed class BranchName
    {
        private readonly string _name;

        public static Maybe<BranchName> TryParse(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return Maybe<BranchName>.Empty();
            }

            return new Maybe<BranchName>(new BranchName(branchName));
        }

        public BranchName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            _name = name;
        }

        public string Name => _name;

        public string Normalize()
        {
            var invalidCharacters = new[] {"/", @"\", "\""};

            string branchNameWithValidCharacters = invalidCharacters.Aggregate(_name,
                                                                               (current, invalidCharacter) =>
                                                                               current.Replace(invalidCharacter, "-"));

            var removedFeatureInName = branchNameWithValidCharacters.Replace("feature-", string.Empty);

            return removedFeatureInName;
        }

        public string LogicalName => BranchHelper.GetLogicalName(Name).Name;

        public string FullName => Name;

        public override string ToString()
        {
            return Name;
        }
    }
}
