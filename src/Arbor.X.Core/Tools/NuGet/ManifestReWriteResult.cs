using System;
using System.Collections.Generic;
using Arbor.Defensive.Collections;

namespace Arbor.X.Core.Tools.NuGet
{
    public class ManifestReWriteResult
    {
        public string UsedPrefix { get; }

        public IReadOnlyCollection<string> RemoveTags { get; }

        public ManifestReWriteResult(IEnumerable<string> removeTags, string usedPrefix)
        {
            if (string.IsNullOrWhiteSpace(usedPrefix))
            {
                throw new ArgumentNullException(nameof(usedPrefix));
            }

            if (removeTags == null)
            {
                throw new ArgumentNullException(nameof(removeTags));
            }

            UsedPrefix = usedPrefix;
            RemoveTags = removeTags.SafeToReadOnlyCollection();
        }
    }
}
