using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Arbor.Defensive.Collections
{
    public static class EnumerableOf<T> where T : class
    {
        private static readonly Lazy<ImmutableArray<T>> LazyEnumerable = new Lazy<ImmutableArray<T>>(Initialize);

        public static ImmutableArray<T> Items => LazyEnumerable.Value;

        private static ImmutableArray<T> Initialize() =>
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsInitOnly && field.GetType() == typeof(T))
                .Select(field => field.GetValue(null) as T)
                .NotNull()
                .ToImmutableArray();
    }
}