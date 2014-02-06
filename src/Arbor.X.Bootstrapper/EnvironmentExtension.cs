namespace Arbor.X.Bootstrapper
{
    public static class EnvironmentExtension
    {
        public static bool TryParseBool(this string value, bool defaultValue)
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