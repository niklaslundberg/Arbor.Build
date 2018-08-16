using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class BuildConfiguration
    {
        public BuildConfiguration([NotNull] string configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(configuration));
            }

            Configuration = configuration;
        }

        public string Configuration { get; }
    }
}
