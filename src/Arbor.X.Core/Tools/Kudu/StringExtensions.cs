using System;

namespace Arbor.Build.Core.Tools.Kudu
{
    public static class StringExtensions
    {
        public static string ExtractFromTag(this string value, string whatToExtract = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            string extracted = value;

            const StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase;

            string message = $"Could not extract {whatToExtract ?? "value"} from text '{value}'";

            var exception = new FormatException(message);

            if (value.Trim().StartsWith("<", StringComparison.Ordinal))
            {
                int indexOf = value.IndexOf(">", comparisonType);

                if (indexOf < 0)
                {
                    throw exception;
                }

                int valueStartIndex = indexOf + 1;

                if (value.Length < valueStartIndex)
                {
                    throw exception;
                }

                string valueAndClosingTag = value.Substring(valueStartIndex);

                int valueEndPosition = valueAndClosingTag.IndexOf("<", comparisonType);

                if (valueEndPosition < 0)
                {
                    throw exception;
                }

                string parsedValue = valueAndClosingTag.Substring(0, valueEndPosition);

                extracted = parsedValue;
            }

            return extracted;
        }
    }
}
