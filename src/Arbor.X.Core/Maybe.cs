using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace Arbor.X.Core
{
    [ImmutableObject(true)]
    public struct Maybe<T> where T : class
    {
        readonly T _value;

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
                        string.Format("Cannot get the instance of type {0} because it has value", typeof (T)));
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

            return (obj is Maybe<T> && Equals((Maybe<T>) obj)) || (obj is T && Equals((T) obj));
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
            var exception =
                new InvalidOperationException(string.Format("Cannot convert a default value of type {0} into a {1} type",
                    typeof (Maybe<T>), typeof (T)));

            if (!maybe.HasValue)
            {
                throw exception;
            }

            return maybe.Value;
        }

        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }

        public static Maybe<T> Empty()
        {
            return new Maybe<T>();
        }
    }
}