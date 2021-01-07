using System;
using System.Collections.Generic;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public abstract class ValueObject<T, TValue> : IEquatable<ValueObject<T, TValue>>
        where T : ValueObject<T, TValue>, IEquatable<ValueObject<T, TValue>> where TValue : IEquatable<TValue>
    {
        private readonly IEqualityComparer<TValue>? _comparer;

        public ValueObject(TValue value, IEqualityComparer<TValue> comparer = default)
        {
            _comparer = comparer;
            Value = value;
        }

        public TValue Value { get; }


        public bool Equals(ValueObject<T, TValue>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(Value, other.Value))
            {
                return true;
            }

            if (_comparer is {})
            {
                return _comparer.Equals(Value, other.Value);
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj is T t)
            {
                return Equals(t);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (_comparer is {})
            {
                return _comparer.GetHashCode(Value);
            }

            return EqualityComparer<TValue>.Default.GetHashCode(Value);
        }

        public static bool operator ==(ValueObject<T, TValue>? left, ValueObject<T, TValue>? right) =>
            Equals(left, right);

        public static bool operator !=(ValueObject<T, TValue>? left, ValueObject<T, TValue>? right) =>
            !Equals(left, right);

        public override string ToString() => Value.ToString() ?? base.ToString()!;
    }
}