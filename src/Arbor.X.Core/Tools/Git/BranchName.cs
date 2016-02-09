using System;
using System.Linq;

namespace Arbor.X.Core.Tools.Git
{
    public sealed class BranchName
    {
        readonly string _name;

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
