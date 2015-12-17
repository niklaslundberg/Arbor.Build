using System;
using System.Collections.Generic;
using System.ComponentModel;

using JetBrains.Annotations;

namespace Arbor.X.Core
{
    [ImmutableObject(true)]
    public struct Maybe<T>
        where T : class
    {
        private readonly T _value;

        public Maybe([CanBeNull] T value = null)
        {
            _value = value;
        }

        /// <exception cref="NullReferenceException" accessor="get">
        ///     Throws exception if the instance has no value. Use HasValue
        ///     before calling Value.
        /// </exception>
        [NotNull]
        public T Value
        {
            get
            {
                if (_value == null)
                {
                    throw new NullReferenceException(
                        $"Cannot get the instance of type {typeof(T)} because it has value null. Make sure to call {nameof(HasValue)} property before access the {nameof(Value)} property");
                }
                return _value;
            }
        }

        public bool HasValue => _value != null;

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
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(_value, obj))
            {
                return true;
            }

            return (obj is Maybe<T> && Equals((Maybe<T>)obj)) || (obj is T && Equals((T)obj));
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
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

        public static bool operator !=(Maybe<T> maybe, T value)
        {
            return !(maybe == value);
        }

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

        public static implicit operator Maybe<T>([CanBeNull]T value)
        {
            return new Maybe<T>(value);
        }

        public static Maybe<T> Empty()
        {
            return new Maybe<T>();
        }
    }
}
