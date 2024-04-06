using System;
using static System.String;

namespace Arbor.Build.Core.BuildVariables;

public class BuildVariable : IVariable
{
    public BuildVariable(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public string? Value { get; }

    public override string ToString() => this.DisplayPair();
}