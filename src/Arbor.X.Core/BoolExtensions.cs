namespace Arbor.X.Core
{
    public static class BoolExtensions
    {
        public static ParseResult<bool> TryParseBool(this string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ParseResult<bool>.Create(defaultValue, false, value);
            }

            bool parsedValue;

            if (!bool.TryParse(value, out parsedValue))
            {
                return ParseResult<bool>.Create(defaultValue, false, value);
            }

            return ParseResult<bool>.Create(parsedValue, true, value);
        }
    }
    public static class IntExtensions
    {
        public static ParseResult<int> TryParseInt32(this string value, int defaultValue = default(int))
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

    public sealed class ParseResult<T>
    {
        public override string ToString()
        {
            if (!typeof (T).IsValueType && Equals(Value, default(T)))
            {
                return string.Empty;
            }

            return Value.ToString();
        }

        public bool Parsed
        {
            get { return _parsed; }
        }

        public T Value
        {
            get { return _value; }
        }

        public string OriginalValue
        {
            get { return _originalValue; }
        }

        readonly bool _parsed;
        readonly T _value;
        readonly string _originalValue;

        ParseResult(bool parsed, T value, string originalValue)
        {
            _parsed = parsed;
            _value = value;
            _originalValue = originalValue;
        }

        public static ParseResult<TResult> Create<TResult>(TResult value, bool parsed, string original)
        {
            return new ParseResult<TResult>(parsed, value, original);
        } 
        
        public static implicit operator T(ParseResult<T> result)
        {
            return result.Value;
        }
    }
}