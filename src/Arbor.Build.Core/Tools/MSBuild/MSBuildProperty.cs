using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class MSBuildProperty
    {
        public MSBuildProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(name));
            }

            Name = name;
            Value = value ?? "";
        }

        public string Name { get; }

        public string Value { get; }

        public override string ToString() => $"[{Name}] = '{Value}'";
    }
}
