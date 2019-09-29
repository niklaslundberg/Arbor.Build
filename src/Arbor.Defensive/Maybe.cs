using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace Arbor.Defensive
{
    [ImmutableObject(true)]
    public struct Maybe<T> : IEquatable<Maybe<T>> where T : class
    {
        private static readonly Lazy<Maybe<T>> _Empty = new Lazy<Maybe<T>>(() => default);

        private readonly T? _value;

        public Maybe(T? value = null) => _value = value;

        /// <summary>
        /// Returns the underlying value of T
        /// </summary>
        /// <exception cref="NullReferenceException" accessor="get">
        ///     Throws exception if the instance has no value. Use HasValue
        ///     before calling Value.
        /// </exception>
        [NotNull]
        public T Value
        {
            get
            {
                CheckForNull();

                return _value;
            }
        }

        private void CheckForNull()
        {
            if (_value == null)
            {
                throw new NullReferenceException(
                    $"Cannot get the instance of type {typeof(T)} because it has value null. Make sure to call {nameof(HasValue)} property before access the {nameof(Value)} property");
            }
        }

        public bool HasValue => _value != null;

        public static implicit operator T(Maybe<T> maybe)
        {
            if (!maybe.HasValue)
            {
                var exception =
                    new InvalidOperationException(
                        $"Cannot convert a default value of type {typeof(Maybe<T>)} into a {typeof(T)} type because the value is null. Make sure to check {nameof(HasValue)} property making an implicit conversion");

                throw exception;
            }

            return maybe.Value;
        }

        public static implicit operator Maybe<T>([CanBeNull] T value)
        {
            if (value is null)
            {
                return Empty();
            }

            return new Maybe<T>(value);
        }

        public static bool operator ==(Maybe<T> left, T right)
        {
            if (right == null)
            {
                return false;
            }

            if (!left.HasValue)
            {
                return false;
            }

            return left.Value.Equals(right);
        }

        public static bool operator !=(Maybe<T> maybe, T value) => !(maybe == value);

        public static bool operator ==(Maybe<T> left, Maybe<T> right)
        {
            if (!left.HasValue)
            {
                return false;
            }

            if (!right.HasValue)
            {
                return false;
            }

            return left.Value.Equals(right.Value);
        }

        public static bool operator !=(Maybe<T> left, Maybe<T> right)
        {
            if (!left.HasValue)
            {
                return true;
            }

            if (!right.HasValue)
            {
                return true;
            }

            return !left.Value.Equals(right.Value);
        }

        public static Maybe<T> Empty() => _Empty.Value;

        public bool Equals(Maybe<T> other)
        {
            if (!other.HasValue)
            {
                return false;
            }

            if (!HasValue)
            {
                return false;
            }

            if (ReferenceEquals(_value, other._value))
            {
                return true;
            }

            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(_value, obj))
            {
                return true;
            }

            return (obj is Maybe<T> maybe && Equals(maybe)) || (obj is T t && Equals(t));
        }

        public override int GetHashCode() => _value?.GetHashCode() ?? 0;

        public T ToT() => throw new NotImplementedException();
    }
}
