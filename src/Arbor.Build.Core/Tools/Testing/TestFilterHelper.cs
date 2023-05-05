using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.Tools.Testing;

public static class TestFilterHelper
{
    public static ImmutableArray<string> AssemblyFilePrefixes(this IReadOnlyCollection<IVariable> buildVariables) =>
        (buildVariables ?? throw new ArgumentNullException(nameof(buildVariables))).GetVariableValueOrDefault(
            WellKnownVariables.TestsAssemblyStartsWith,
            string.Empty)!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
}