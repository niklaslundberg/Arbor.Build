using System;

namespace Arbor.Build.Core.BuildVariables;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class VariableDescriptionAttribute : Attribute
{
    public string? PreferUse { get; }

    public VariableDescriptionAttribute(string description, string? defaultValue = null, string? preferUse = null)
    {
        Description = description;
        DefaultValue = defaultValue;
        PreferUse = preferUse;
    }

    public string Description { get; }

    public string? DefaultValue { get; }
}