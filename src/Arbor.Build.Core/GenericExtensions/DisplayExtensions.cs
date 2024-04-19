using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arbor.Build.Core.GenericExtensions;

public static class DisplayExtensions
{
    public static string DisplayAsTable(
        this IEnumerable<IDictionary<string, string?>> dictionaries,
        char padChar = '.')
    {
        ArgumentNullException.ThrowIfNull(dictionaries);
        IReadOnlyCollection<IDictionary<string, string?>> materialized = dictionaries.SafeToReadOnlyCollection();

        if (materialized.Count == 0)
        {
            return string.Empty;
        }

        const char separator = ' ';
        const string noValue = "-";

        string[] allKeys =
            materialized.SelectMany(dictionary => dictionary.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var maxLengths =
            allKeys.Select(
                    key => new
                    {
                        Key = key,
                        MaxLength = Math.Max(
                            key.Length,
                            Math.Max(
                                materialized.Where(dictionary => dictionary.ContainsKey(key))
                                    .Select(dictionary => dictionary[key])
                                    .Max(value => value?.Length ?? 0),
                                noValue.Length))
                    })
                .ToArray();

        var builder = new StringBuilder();

        foreach (string title in allKeys)
        {
            int padding = maxLengths.Single(keyLengths => keyLengths.Key == title).MaxLength - title.Length + 1;

            if (padding > 0)
            {
                builder.Append(title).Append(new string(separator, padding));
            }
        }

        builder.AppendLine();

        foreach (IDictionary<string, string?> dictionary in materialized)
        {
            var indexedKeys = allKeys.Select((item, index) => new { Key = item, Index = index }).ToArray();

            foreach (var indexedKey in indexedKeys)
            {
                string value = dictionary.TryGetValue(indexedKey.Key, out string? foundValue) ? foundValue ?? "" : noValue;

                if (string.IsNullOrWhiteSpace(value))
                {
                    value = noValue;
                }

                int maxLength = maxLengths.Single(max => max.Key == indexedKey.Key).MaxLength;

                int padLength = maxLength - value.Length;

                if (indexedKey.Index >= 1)
                {
                    builder.Append(separator);
                }

                builder.Append(value);
                bool isNotLast = indexedKey.Index < indexedKeys.Length - 1;

                if (isNotLast && padLength > 0)
                {
                    builder.Append(new string(padChar, padLength));
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}