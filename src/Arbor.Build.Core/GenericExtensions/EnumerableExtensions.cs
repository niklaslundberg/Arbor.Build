using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Arbor.Build.Core.GenericExtensions;

public static class EnumerableExtensions
{
    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        if (enumerable is ImmutableArray<T> array)
        {
            return array.IsDefault ? [] : array;
        }

        if (enumerable is List<T> list)
        {
            return list;
        }

        return enumerable.ToImmutableArray();
    }
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> items) where T : class
    {
        foreach (var item in items)
        {
            if (item is { })
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<ValueTuple<T1, T2, T3>> NotNull<T1, T2, T3>(
        this IEnumerable<ValueTuple<T1?, T2?, T3?>> items) where T1 : class where T2 : class where T3 : class
    {
        foreach (var item in items)
        {
            if (item.Item1 is { } && item.Item2 is { } && item.Item3 is { })
            {
                yield return (item.Item1, item.Item2, item.Item3);
            }
        }
    }

    public static IEnumerable<ValueTuple<T1, T2>> NotNull<T1, T2>(this IEnumerable<ValueTuple<T1?, T2?>> items)
        where T1 : class where T2 : class
    {
        foreach (var item in items)
        {
            if (item.Item1 is { } && item.Item2 is { })
            {
                yield return (item.Item1, item.Item2);
            }
        }
    }

    public static ImmutableArray<T> SafeToReadOnlyCollection<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable is null)
        {
            return [];
        }

        if (enumerable is ImmutableArray<T> array)
        {
            return array.IsDefault ? [] : array;
        }

        return enumerable.ToImmutableArray();
    }

}