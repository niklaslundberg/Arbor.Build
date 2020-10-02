using System;
using System.Collections.Immutable;
using System.Linq;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class PackageUploadFilter
    {
        public ImmutableArray<string> Exclusions { get; }
        private readonly StringComparison _stringComparison;

        public PackageUploadFilter(string startsWithExclusions, IFileSystem fileSystem)
        {
            _stringComparison = fileSystem.GetPathComparison();
            Exclusions = startsWithExclusions.Split(";").ToImmutableArray();
        }


        public bool UploadEnable(string packageFile) => !Exclusions.Any(exclusion => packageFile.StartsWith(exclusion, _stringComparison));
    }
}