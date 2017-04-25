using System;

namespace Arbor.X.Core.BuildVariables
{
    public sealed class VariableDescription : IEquatable<VariableDescription>
    {
        private readonly string _defaultValue;
        private readonly string _description;
        private readonly string _wellknownName;

        private VariableDescription(string invariantName, string description, string wellknownName, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(invariantName))
            {
                throw new ArgumentNullException(nameof(invariantName));
            }

            InvariantName = invariantName;
            _description = description;
            _wellknownName = wellknownName;
            _defaultValue = defaultValue;
        }

        public string WellknownName
        {
            get { return _wellknownName ?? string.Empty; }
        }

        public string DefaultValue
        {
            get { return _defaultValue ?? string.Empty; }
        }

        public string InvariantName { get; }

        public string Description
        {
            get { return _description ?? string.Empty; }
        }

        public bool Equals(VariableDescription other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(InvariantName, other.InvariantName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is VariableDescription && Equals((VariableDescription)obj);
        }

        public override int GetHashCode()
        {
            return (InvariantName != null ? InvariantName.GetHashCode() : 0);
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
            return $"{InvariantName} ({WellknownName}) [{DefaultValue}], {Description}";
        }
    }
}
