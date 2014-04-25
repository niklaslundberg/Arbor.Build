namespace Arbor.X.Core
{
    public static class BoolExtensions
    {
        public static bool TryParseBool(this string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            bool parsedValue;

            if (!bool.TryParse(value, out parsedValue))
            {
                return defaultValue;
            }

            return parsedValue;
        }
    }
}