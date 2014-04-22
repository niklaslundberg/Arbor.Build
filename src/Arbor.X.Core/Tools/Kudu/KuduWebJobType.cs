using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.Tools.Kudu
{
    internal class KuduWebJobType : IEquatable<KuduWebJobType>
    {
        readonly string _invariantName;

        KuduWebJobType(string invariantName)
        {
            _invariantName = invariantName;
        }

        public static KuduWebJobType Continuous
        {
            get { return new KuduWebJobType("Continuous"); }
        }

        public static KuduWebJobType Triggered
        {
            get { return new KuduWebJobType("Continuous"); }
        }

        public static IEnumerable<KuduWebJobType> All
        {
            get
            {
                yield return Continuous;
                yield return Triggered;
            }
        }

        public bool Equals(KuduWebJobType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_invariantName, other._invariantName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KuduWebJobType) obj);
        }

        public override int GetHashCode()
        {
            return (_invariantName != null ? _invariantName.GetHashCode() : 0);
        }

        public static bool operator ==(KuduWebJobType left, KuduWebJobType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KuduWebJobType left, KuduWebJobType right)
        {
            return !Equals(left, right);
        }

        public static KuduWebJobType Parse(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException("type");
            }

            KuduWebJobType foundItem =
                All.SingleOrDefault(
                    item => item._invariantName.Equals(type, StringComparison.InvariantCultureIgnoreCase));

            if (foundItem == null)
            {
                throw new InvalidOperationException(string.Format("Could not parse {0} from value '{1}'",
                    typeof (KuduWebJobType).Name, type));
            }

            return foundItem;
        }
    }
}