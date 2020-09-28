using System;
using System.Collections.Immutable;
using System.Linq;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class PackageUploadFilter
    {
        private readonly ImmutableArray<string> _exclusions;
        private readonly StringComparison _stringComparison;

        public PackageUploadFilter(string startsWithExclusions, IFileSystem fileSystem)
        {
            _stringComparison = fileSystem.GetPathComparison();
            _exclusions = startsWithExclusions.Split(";").ToImmutableArray();
        }


        public bool UploadEnable(string packageFile) => !_exclusions.Any(exclusion => packageFile.StartsWith(exclusion, _stringComparison));
    }
}