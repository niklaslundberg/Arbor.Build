namespace Arbor.Build.Core.GenericExtensions.Boolean
{
    public static class BoolExtensions
    {
        public static bool ParseOrDefault(this string value, bool defaultValue = false)
        {
            if (!bool.TryParse(value, out bool result))
            {
                return defaultValue;
            }

            return result;
        }

        public static bool TryParseBool(this string value, out bool result, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = defaultValue;
                return false;
            }

            if (!bool.TryParse(value, out bool parsedValue))
            {
                result = defaultValue;
                return false;
            }

            result = parsedValue;
            return true;
        }
    }
}
