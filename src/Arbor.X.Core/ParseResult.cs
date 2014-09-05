namespace Arbor.X.Core
{
    public sealed class ParseResult<T>
    {
        readonly string _originalValue;
        readonly bool _parsed;
        readonly T _value;

        ParseResult(bool parsed, T value, string originalValue)
        {
            _parsed = parsed;
            _value = value;
            _originalValue = originalValue;
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

        public override string ToString()
        {
            if (!typeof (T).IsValueType && Equals(Value, default(T)))
            {
                return string.Empty;
            }

            return Value.ToString();
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