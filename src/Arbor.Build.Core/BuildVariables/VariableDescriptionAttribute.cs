using System;

namespace Arbor.Build.Core.BuildVariables;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class VariableDescriptionAttribute(
    string description,
    string? defaultValue = null,
    string? preferUse = null)
    : Attribute
{
    public string? PreferUse { get; } = preferUse;

    public string Description { get; } = description;

    public string? DefaultValue { get; } = defaultValue;
}