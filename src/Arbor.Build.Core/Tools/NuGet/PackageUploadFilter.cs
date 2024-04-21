using System;
using System.Collections.Immutable;
using System.Linq;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

public class PackageUploadFilter(string startsWithExclusions, IFileSystem fileSystem)
{
    public ImmutableArray<string> Exclusions { get; } = startsWithExclusions.Split(';', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

    private readonly StringComparison _stringComparison = fileSystem.GetPathComparison();


    public bool UploadEnable(string packageFile) => !Exclusions.Any(exclusion => packageFile.StartsWith(exclusion, _stringComparison));
}