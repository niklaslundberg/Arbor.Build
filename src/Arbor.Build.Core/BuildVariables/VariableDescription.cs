using System;

namespace Arbor.Build.Core.BuildVariables;

public sealed class VariableDescription : IEquatable<VariableDescription>
{
    private readonly string? _defaultValue;
    private readonly string? _description;
    private readonly string? _wellKnownName;

    private VariableDescription(string invariantName, string? description, string? wellKnownName, string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(invariantName))
        {
            throw new ArgumentNullException(nameof(invariantName));
        }

        InvariantName = invariantName;
        _description = description;
        _wellKnownName = wellKnownName;
        _defaultValue = defaultValue;
    }

    public string WellKnownName => _wellKnownName ?? string.Empty;

    public string DefaultValue => _defaultValue ?? string.Empty;

    public string InvariantName { get; }

    public string Description => _description ?? string.Empty;

    public static implicit operator string(VariableDescription variableDescription) => variableDescription.InvariantName;

    public static implicit operator VariableDescription(string invariantName) => Create(invariantName);
    public static string FromString(string invariantName) => Create(invariantName);

    public static bool operator ==(VariableDescription left, VariableDescription right) => Equals(left, right);

    public static bool operator !=(VariableDescription left, VariableDescription right) => !Equals(left, right);

    public static VariableDescription Create(string invariantName, string? description = null,
        string? wellKnownName = null, string? defaultValue = null) =>
        new(invariantName, description, wellKnownName, defaultValue);

    public bool Equals(VariableDescription? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(InvariantName, other.InvariantName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is VariableDescription description && Equals(description);
    }

    public override int GetHashCode() => InvariantName.GetHashCode(StringComparison.InvariantCulture);

    public override string ToString() => $"{InvariantName} ({WellKnownName}) [{DefaultValue}], {Description}";
}