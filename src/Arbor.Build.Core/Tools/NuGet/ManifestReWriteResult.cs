using System;
using System.Collections.Generic;
using Arbor.Defensive.Collections;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

public class ManifestReWriteResult
{
    public ManifestReWriteResult(IEnumerable<string> removeTags, string usedPrefix, FileEntry? rewrittenNuSpec)
    {
        if (string.IsNullOrWhiteSpace(usedPrefix))
        {
            throw new ArgumentNullException(nameof(usedPrefix));
        }

        ArgumentNullException.ThrowIfNull(removeTags);

        UsedPrefix = usedPrefix;
        RewrittenNuSpec = rewrittenNuSpec;
        RemoveTags = removeTags.SafeToReadOnlyCollection();
    }

    public string UsedPrefix { get; }
    public FileEntry? RewrittenNuSpec { get; }

    public IReadOnlyCollection<string> RemoveTags { get; }
}