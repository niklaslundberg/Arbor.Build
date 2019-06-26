using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.BuildVariables
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

        public string WellknownName => _wellknownName ?? string.Empty;

        public string DefaultValue => _defaultValue ?? string.Empty;

        public string InvariantName { get; }

        public string Description => _description ?? string.Empty;

        public static implicit operator string([NotNull] VariableDescription variableDescription)
        {
            if (variableDescription == null)
            {
                throw new ArgumentNullException(nameof(variableDescription));
            }

            return variableDescription.InvariantName;
        }

        public static implicit operator VariableDescription(string invariantName)
        {
            return Create(invariantName);
        }

        public static bool operator ==(VariableDescription left, VariableDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(VariableDescription left, VariableDescription right)
        {
            return !Equals(left, right);
        }

        public static VariableDescription Create(
            string invariantName,
            string description = null,
            string wellknownName = null,
            string defaultValue = null)
        {
            return new VariableDescription(invariantName, description, wellknownName, defaultValue);
        }

        public bool Equals(VariableDescription other)
        {
            if (other is null)
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
            if (obj is null)
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
            return InvariantName != null ? InvariantName.GetHashCode(StringComparison.InvariantCulture) : 0;
        }

        public override string ToString()
        {
            return $"{InvariantName} ({WellknownName}) [{DefaultValue}], {Description}";
        }
    }
}
