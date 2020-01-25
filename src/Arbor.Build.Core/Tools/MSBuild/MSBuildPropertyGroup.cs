using System;
using System.Collections.Immutable;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class MSBuildPropertyGroup
    {
        public MSBuildPropertyGroup(ImmutableArray<MSBuildProperty> properties)
        {
            if (properties.IsDefault)
            {
                throw new ArgumentException(Resources.ImmutableArrayCannotBeDefault, nameof(properties));
            }

            Properties = properties;
        }

        public ImmutableArray<MSBuildProperty> Properties { get; }
    }
}
