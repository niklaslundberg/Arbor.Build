using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arbor.X.Core
{
    public static class DisplayExtensions
    {
        public static string DisplayAsTable(this IEnumerable<IDictionary<string, string>> dictionaries,
            char padChar = '.')
        {
            if (dictionaries == null)
            {
                return string.Empty;
            }

            const char separator = ' ';
            const string noValue = "-";

            IDictionary<string, string>[] dicts = dictionaries.ToArray();

            string[] allKeys =
                dicts.SelectMany(dictionary => dictionary.Keys)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToArray();

            var maxLengths =
                allKeys.Select(
                    key => new
                           {
                               Key = key,
                               MaxLength = Math.Max(key.Length, Math.Max(
                                   dicts.Where(dictionary => dictionary.ContainsKey(key))
                                       .Select(dictionary => dictionary[key])
                                       .Select(value => (value ?? "").Length)
                                       .Max(), noValue.Length))
                           })
                    .ToArray();

            var builder = new StringBuilder();

            foreach (string title in allKeys)
            {
                int padding = maxLengths.Single(keyLengths => keyLengths.Key == title).MaxLength - title.Length + 1;

                if (padding > 0)
                {
                    builder.Append(title + new string(separator, padding));
                }
            }
            builder.AppendLine();

            foreach (IDictionary<string, string> dictionary in dicts)
            {
                var indexedKeys = allKeys.Select((item, index) => new {Key = item, Index = index}).ToArray();

                foreach (var indexedKey in indexedKeys)
                {
                    string value = dictionary.ContainsKey(indexedKey.Key) ? dictionary[indexedKey.Key] : noValue;

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
                    var isNotLast = indexedKey.Index < indexedKeys.Length - 1;

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
}