using System;
using JetBrains.Annotations;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.Versioning
{
    public static class SemVerExtensions
    {
        public static string Suffix([NotNull] this SemanticVersion semanticVersion)
        {
            if (semanticVersion == null)
            {
                throw new ArgumentNullException(nameof(semanticVersion));
            }


            ReadOnlySpan<char> normalized = semanticVersion.ToNormalizedString().AsSpan();

            int dashIndex = normalized.IndexOf('-');

            if (dashIndex < 0)
            {
                return string.Empty;
            }

            string metadata = semanticVersion.HasMetadata ? $"+{semanticVersion.Metadata}" : null;

            ReadOnlySpan<char> suffix = normalized.Slice(dashIndex + 1);

            return suffix.ToString() + metadata;
        }
    }
}
