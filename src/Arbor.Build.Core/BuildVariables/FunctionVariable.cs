using System;

namespace Arbor.Build.Core.BuildVariables;

public class FunctionVariable : IVariable
{
    private readonly Func<string?> _getValue;

    public FunctionVariable(string key, Func<string?> getValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _getValue = getValue;

        Key = key;
    }

    public string Key { get; }

    public string? Value => _getValue.Invoke();
}