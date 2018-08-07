
namespace Arbor.X.Core.GenericExtensions
{
    public static class IntExtensions
    {
        public static int ParseOrDefault(this string value, int defaultValue = default)
        {
            if (!int.TryParse(value, out int result))
            {
                return defaultValue;
            }

            return result;
        }

        public static bool TryParseInt32(this string value, out int result, int defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = defaultValue;
                return false;
            }

            if (!int.TryParse(value, out int parsedValue))
            {
                result = defaultValue;
                return false;
            }

            result = parsedValue;
            return true;
        }
    }
}
