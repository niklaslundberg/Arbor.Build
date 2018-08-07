using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.GenericExtensions
{
    public static class StringExtensions
    {
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
