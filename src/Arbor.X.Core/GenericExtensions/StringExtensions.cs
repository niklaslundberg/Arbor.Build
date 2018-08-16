using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.X.Core.GenericExtensions
{
    public static class StringExtensions
    {
        public static bool StartsWithAny(
            [NotNull] this string value,
            [ItemNotNull] [NotNull] IReadOnlyCollection<string> whatToFind,
            StringComparison stringComparison)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (whatToFind == null)
            {
                throw new ArgumentNullException(nameof(whatToFind));
            }

            return whatToFind.Any(current => value.StartsWith(current, stringComparison));
        }

        public static bool TryParseString(this string value, out string result, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = defaultValue;
                return false;
            }

            result = value;
            return true;
        }

        public static string LeftPad(this string value, int totaltLenght, char character)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string(character, totaltLenght);
            }

            if (totaltLenght <= 0)
            {
                return value;
            }

            int lengthDiff = totaltLenght - value.Length;

            if (lengthDiff <= 0)
            {
                return value;
            }

            return value.PadLeft(totaltLenght, character);
        }

        public static string Wrap(this string text, string wrap)
        {
            return wrap + text + wrap;
        }

        public static IEnumerable<string> WrapItems(this IEnumerable<string> enumerable, string wrap)
        {
            return enumerable.Select(item => item.Wrap(wrap));
        }
    }
}
