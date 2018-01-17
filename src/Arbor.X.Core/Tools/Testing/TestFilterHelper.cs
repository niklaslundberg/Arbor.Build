using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.X.Core.BuildVariables;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Testing
{
    public static class TestFilterHelper
    {
        public static ImmutableArray<string> AssemblyFilePrefixes([NotNull] this IReadOnlyCollection<IVariable> buildVairables)
        {
            if (buildVairables == null)
            {
                throw new ArgumentNullException(nameof(buildVairables));
            }

            ImmutableArray<string> filters = buildVairables
                .GetVariableValueOrDefault(WellKnownVariables.TestsAssemblyStartsWith, string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

            return filters;
        }
    }
}
