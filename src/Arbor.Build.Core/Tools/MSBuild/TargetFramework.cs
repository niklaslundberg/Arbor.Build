using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class TargetFramework : ValueObject<TargetFramework, string>
    {
        public static readonly TargetFramework Net47 = new TargetFramework("net47");
        public static readonly TargetFramework Net48 = new TargetFramework("net48");
        public static readonly TargetFramework Net471 = new TargetFramework("net471");
        public static readonly TargetFramework Net472 = new TargetFramework("net472");
        public static readonly TargetFramework NetStandard2_0 = new TargetFramework("netstandard2.0");
        public static readonly TargetFramework NetStandard2_1 = new TargetFramework("netstandard2.1");
        public static readonly TargetFramework NetCoreApp3_1 = new TargetFramework("netcoreapp3.1");
        public static readonly TargetFramework NetCoreApp3_0 = new TargetFramework("netcoreapp3.0");
        public static readonly TargetFramework Net5_0 = new TargetFramework("net5.0");
        public static readonly TargetFramework Empty = new TargetFramework("N/A");

        public override string ToString() => Value;

        public TargetFramework([NotNull] string value) : base(value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }
        }
    }
}