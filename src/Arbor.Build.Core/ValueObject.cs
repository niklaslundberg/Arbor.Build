using System;
using System.Collections.Generic;

namespace Arbor.Build.Core;

public abstract class ValueObject<T, TValue>(TValue value, IEqualityComparer<TValue>? comparer = default)
    : IEquatable<ValueObject<T, TValue>>
    where T : ValueObject<T, TValue>, IEquatable<ValueObject<T, TValue>>
    where TValue : IEquatable<TValue>
{
    public TValue Value { get; } = value;


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

        if (comparer is {})
        {
            return comparer.Equals(Value, other.Value);
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
        if (comparer is {})
        {
            return comparer.GetHashCode(Value);
        }

        return EqualityComparer<TValue>.Default.GetHashCode(Value);
    }

    public static bool operator ==(ValueObject<T, TValue>? left, ValueObject<T, TValue>? right) =>
        Equals(left, right);

    public static bool operator !=(ValueObject<T, TValue>? left, ValueObject<T, TValue>? right) =>
        !Equals(left, right);

    public override string ToString() => Value.ToString() ?? base.ToString()!;
}