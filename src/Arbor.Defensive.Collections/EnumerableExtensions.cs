using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Arbor.Defensive.Collections;

public static class EnumerableExtensions
{
    public static ImmutableArray<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is null)
        {
            throw new ArgumentNullException(nameof(enumerable));
        }

        if (enumerable is ImmutableArray<T> array)
        {
            return array.IsDefault ? ImmutableArray<T>.Empty : array;
        }

        var immutableArray = enumerable.ToImmutableArray();

        return immutableArray;
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> items) where T : class
    {
        foreach (var item in items)
        {
            if (item is {})
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
            if (item.Item1 is {} && item.Item2 is {} && item.Item3 is {})
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
            if (item.Item1 is {} && item.Item2 is {})
            {
                yield return (item.Item1, item.Item2);
            }
        }
    }

    public static ImmutableArray<T> SafeToReadOnlyCollection<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable is null)
        {
            return ImmutableArray<T>.Empty;
        }

        if (enumerable is ImmutableArray<T> array)
        {
            return array.IsDefault ? ImmutableArray<T>.Empty : array;
        }

        var immutableArray = enumerable.ToImmutableArray();

        return immutableArray;
    }

    public static ImmutableArray<T> ValueToImmutableArray<T>(this T item)
    {
        var immutableArray = new[] {item}.ToImmutableArray();

        return immutableArray;
    }

}