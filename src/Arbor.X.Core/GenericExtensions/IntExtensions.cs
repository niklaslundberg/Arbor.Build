using Arbor.X.Core.Parsing;

namespace Arbor.X.Core.GenericExtensions
{
    public static class IntExtensions
    {
        public static ParseResult<int> TryParseInt32(this string value, int defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ParseResult<int>.Create(defaultValue, false, value);
            }

            int parsedValue;

            if (!int.TryParse(value, out parsedValue))
            {
                return ParseResult<int>.Create(defaultValue, false, value);
            }

            return ParseResult<int>.Create(parsedValue, true, value);
        }
    }
}
