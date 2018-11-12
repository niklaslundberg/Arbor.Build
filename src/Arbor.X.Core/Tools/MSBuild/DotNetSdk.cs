using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class DotNetSdk : IEquatable<DotNetSdk>
    {
        public static readonly DotNetSdk DotnetWeb = new DotNetSdk("Microsoft.NET.Sdk.Web");
        public static readonly DotNetSdk Dotnet = new DotNetSdk("Microsoft.NET.Sdk");
        public static readonly DotNetSdk None = new DotNetSdk("N/A");

        private static readonly Lazy<ImmutableArray<DotNetSdk>> _LazyAll =
            new Lazy<ImmutableArray<DotNetSdk>>(() => new[]
            {
                None,
                Dotnet,
                DotnetWeb
            }.ToImmutableArray());

        private DotNetSdk([NotNull] string sdkName)
        {
            if (string.IsNullOrWhiteSpace(sdkName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sdkName));
            }

            SdkName = sdkName;
        }

        public string SdkName { get; }

        public static bool operator ==(DotNetSdk left, DotNetSdk right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DotNetSdk left, DotNetSdk right)
        {
            return !Equals(left, right);
        }

        public static ImmutableArray<DotNetSdk> All => _LazyAll.Value;

        public static DotNetSdk ParseOrDefault(string sdkValue)
        {
            if (string.IsNullOrWhiteSpace(sdkValue))
            {
                return None;
            }

            return All.SingleOrDefault(sdk => sdk.SdkName.Equals(sdkValue, StringComparison.Ordinal));
        }

        public bool Equals(DotNetSdk other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(SdkName, other.SdkName, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is DotNetSdk sdk && Equals(sdk);
        }

        public override int GetHashCode()
        {
            return SdkName.GetHashCode(StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return SdkName;
        }
    }
}
