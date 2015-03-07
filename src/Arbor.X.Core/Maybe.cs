using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Arbor.X.Core
{
    public struct Maybe<T> where T : class
    {
        readonly T _item;

        public Maybe([CanBeNull] T item = null)
        {
            _item = item;
        }

        [NotNull]
        public T Item
        {
            get
            {
                if (_item == null)
                {
                    throw new NullReferenceException(
                        string.Format("Cannot get the instance of type {0} because it has value", typeof (T)));
                }
                return _item;
            }
        }

        public bool HasValue => _item != null;

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

            if (ReferenceEquals(_item, other.Item))
            {
                return true;
            }

            return EqualityComparer<T>.Default.Equals(_item, other._item);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(_item, obj))
            {
                return true;
            }

            return (obj is Maybe<T> && Equals((Maybe<T>) obj)) || (obj is T && Equals((T) obj));
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(_item);
        }

        public static bool operator ==(Maybe<T> left, T right)
        {
            if (!left.HasValue)
            {
                return false;
            }

            return left.Item.Equals(right);
        }

        public static bool operator !=(Maybe<T> left, T right)
        {
            if (!left.HasValue)
            {
                return true;
            }

            if (right == null)
            {
                return true;
            }

            return !left.Item.Equals(right);
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

            return left.Item.Equals(right.Item);
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

            return !left.Item.Equals(right.Item);
        }

        public static implicit operator T(Maybe<T> maybe)
        {
            var exception =
                new InvalidOperationException(string.Format("Cannot convert a default value of type{0} into a {1}",
                    typeof (Maybe<T>), typeof (T)));

            if (!maybe.HasValue)
            {
                throw exception;
            }

            return maybe.Item;
        }

        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }
    }
}