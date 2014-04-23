using System;

namespace Arbor.X.Core.Tools.Kudu
{
    public static class StringExtensions
    {
        public static string ExtractFromTag(this string value, string whatToExtract = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var extracted = value;

            const StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase;
            
            var message = string.Format("Could not extract {0} from text '{1}'",
                whatToExtract ?? "value", value);

            var exception = new FormatException(message);

            if (value.Trim().StartsWith("<"))
            {
                var indexOf = value.IndexOf(">", comparisonType);

                if (indexOf < 0)
                {
                    throw exception;
                }

                var valueStartIndex = indexOf + 1;

                if (value.Length < valueStartIndex)
                {
                    throw exception;
                }

                var valueAndClosingTag = value.Substring(valueStartIndex);

                var valueEndPosition = valueAndClosingTag.IndexOf("<", comparisonType);

                if (valueEndPosition < 0)
                {
                    throw exception;
                }

                var parsedValue = valueAndClosingTag.Substring(0, valueEndPosition);

                extracted = parsedValue;
            }

            return extracted;
        }
    }
}