using JetBrains.Annotations;

namespace Arbor.X.Core
{
    public sealed class ParseResult<T>
    {
        ParseResult(bool parsed, [CanBeNull] T value, [CanBeNull] string originalValue)
        {
            Parsed = parsed;
            Value = value;
            OriginalValue = originalValue;
        }

        public bool Parsed { get; }

        public T Value { get; }

        public string OriginalValue { get; }

        public override string ToString()
        {
            if (!typeof(T).IsValueType && Equals(Value, default(T)))
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
