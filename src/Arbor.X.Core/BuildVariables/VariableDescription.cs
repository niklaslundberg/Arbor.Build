using System;

namespace Arbor.X.Core.BuildVariables
{
    public sealed class VariableDescription : IEquatable<VariableDescription>
    {
        readonly string _defaultValue;
        readonly string _description;
        readonly string _invariantName;
        readonly string _wellknownName;

        VariableDescription(string invariantName, string description, string wellknownName, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(invariantName))
            {
                throw new ArgumentNullException("invariantName");
            }

            _invariantName = invariantName;
            _description = description;
            _wellknownName = wellknownName;
            _defaultValue = defaultValue;
        }

        public string WellknownName
        {
            get { return _wellknownName ?? ""; }
        }

        public string DefaultValue
        {
            get { return _defaultValue ?? ""; }
        }

        public string InvariantName
        {
            get { return _invariantName; }
        }

        public string Description
        {
            get { return _description ?? ""; }
        }

        public bool Equals(VariableDescription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_invariantName, other._invariantName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is VariableDescription && Equals((VariableDescription) obj);
        }

        public override int GetHashCode()
        {
            return (_invariantName != null ? _invariantName.GetHashCode() : 0);
        }

        public static bool operator ==(VariableDescription left, VariableDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(VariableDescription left, VariableDescription right)
        {
            return !Equals(left, right);
        }

        public static VariableDescription Create(string invariantName, string description = null,
            string wellknownName = null, string defaultValue = null)
        {
            return new VariableDescription(invariantName, description, wellknownName, defaultValue);
        }

        public static implicit operator string(VariableDescription variableDescription)
        {
            return variableDescription.InvariantName;
        }

        public static implicit operator VariableDescription(string invariantName)
        {
            return Create(invariantName);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}) [{2}], {3}", InvariantName, WellknownName, DefaultValue, Description);
        }
    }
}