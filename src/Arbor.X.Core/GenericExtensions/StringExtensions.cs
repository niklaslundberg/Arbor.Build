using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.Parsing;

namespace Arbor.X.Core.GenericExtensions
{
    public static class StringExtensions
    {
        public static ParseResult<string> TryParseString(this string value, string defaultValue = "")
        {
            if (value == null)
            {
                return ParseResult<string>.Create(defaultValue, false, null);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return ParseResult<string>.Create(defaultValue, false, value);
            }

            return ParseResult<string>.Create(value, true, value);
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
