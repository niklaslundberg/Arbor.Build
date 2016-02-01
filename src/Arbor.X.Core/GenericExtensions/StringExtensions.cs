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

            var lengthDiff = totaltLenght - value.Length;

            if (lengthDiff <= 0)
            {
                return value;
            }

            return value.PadLeft(totaltLenght, character);
        }
    }
}
