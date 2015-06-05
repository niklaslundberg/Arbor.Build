using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Arbor.X.Core
{
    public static class EnumerableExtensions
    {
        public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (enumerable is List<T>)
            {
                return new ReadOnlyCollection<T>((List<T>)enumerable);
            }

            return new ReadOnlyCollection<T>(enumerable.ToList());
        }

        public static IReadOnlyCollection<T> SafeToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return Enumerable.Empty<T>().ToReadOnlyCollection();
            }

            if (enumerable is List<T>)
            {
                return new ReadOnlyCollection<T>((List<T>)enumerable);
            }

            return new ReadOnlyCollection<T>(enumerable.ToList());
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var item in enumerable)
            {
                var current = item;

                action(current);
            }
        }
    }
}