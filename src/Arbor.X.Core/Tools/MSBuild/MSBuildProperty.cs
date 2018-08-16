using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class MSBuildProperty
    {
        public MSBuildProperty([NotNull] string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Value = value ?? "";
        }

        public string Name { get; }
        public string Value { get; }
    }
}
