using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Arbor.Build.Core.GenericExtensions;

public static class EnumerableOf<T> where T : class
{
    private static readonly Lazy<ImmutableArray<T>> LazyEnumerable = new(Initialize);

    public static IReadOnlyCollection<T> Items => LazyEnumerable.Value;

    public static IReadOnlyCollection<T> Empty => ImmutableArray<T>.Empty;

    private static ImmutableArray<T> Initialize() =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsInitOnly && field.FieldType == typeof(T))
            .Select(field => field.GetValue(null) as T)
            .NotNull()
            .ToImmutableArray();
}