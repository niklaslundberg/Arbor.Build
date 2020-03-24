using System;
using System.Collections.Generic;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class BuildContext
    {
        public BuildConfiguration CurrentBuildConfiguration { get; set; }

        public HashSet<string> Configurations { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
