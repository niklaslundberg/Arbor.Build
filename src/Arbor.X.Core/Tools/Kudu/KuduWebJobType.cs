using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.Build.Core.Tools.Kudu
{
    public class KuduWebJobType : IEquatable<KuduWebJobType>
    {
        private KuduWebJobType(string invariantName)
        {
            DisplayName = invariantName;
        }

        public string DisplayName { get; }

        public static bool operator ==(KuduWebJobType left, KuduWebJobType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KuduWebJobType left, KuduWebJobType right)
        {
            return !Equals(left, right);
        }

        public static KuduWebJobType Continuous => new KuduWebJobType("Continuous");

        public static KuduWebJobType Triggered => new KuduWebJobType("Triggered");

        public static IEnumerable<KuduWebJobType> All
        {
            get
            {
                yield return Continuous;
                yield return Triggered;
            }
        }

        public static KuduWebJobType Parse(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            string message = $"Could not parse {typeof(KuduWebJobType).Name} from value '{type}'";

            const StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;

            var exception = new FormatException(message);

            string valueToParse = type.Trim().StartsWith("<", StringComparison.Ordinal)
                ? type.ExtractFromTag(typeof(KuduWebJobType).Name)
                : type;

            KuduWebJobType foundItem =
                All.SingleOrDefault(
                    item => item.DisplayName.Equals(valueToParse, comparisonType));

            if (foundItem == null)
            {
                throw exception;
            }

            return foundItem;
        }

        public bool Equals(KuduWebJobType other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((KuduWebJobType)obj);
        }

        public override int GetHashCode()
        {
            return DisplayName?.GetHashCode(StringComparison.InvariantCulture) ?? 0;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
