using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.Build.Core.BuildVariables;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.Testing
{
    public static class TestFilterHelper
    {
        public static ImmutableArray<string> AssemblyFilePrefixes(
            [NotNull] this IReadOnlyCollection<IVariable> buildVariables)
        {
            if (buildVariables == null)
            {
                throw new ArgumentNullException(nameof(buildVariables));
            }

            ImmutableArray<string> filters = buildVariables
                .GetVariableValueOrDefault(WellKnownVariables.TestsAssemblyStartsWith, string.Empty)!
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

            return filters;
        }
    }
}
