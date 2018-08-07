using System;
using System.Collections.Immutable;

namespace Arbor.X.Core.Tools.MSBuild
{
    public class MSBuildPropertyGroup
    {
        public MSBuildPropertyGroup(ImmutableArray<MSBuildProperty> properties)
        {
            if (properties.IsDefault)
            {
                throw new ArgumentException("Immutable array cannot be default", nameof(properties));
            }

            Properties = properties;
        }

        public ImmutableArray<MSBuildProperty> Properties { get; }
    }
}
