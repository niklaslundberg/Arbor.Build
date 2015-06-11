namespace Arbor.X.Core
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
    }
}