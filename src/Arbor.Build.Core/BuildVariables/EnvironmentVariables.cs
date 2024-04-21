using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Arbor.Build.Core.BuildVariables;

public class EnvironmentVariables : IEnvironmentVariables
{
    public static readonly EnvironmentVariables Empty = new();

    private readonly ConcurrentDictionary<string, string?> _pairs = new(StringComparer.OrdinalIgnoreCase);

    public string? GetEnvironmentVariable(string key) =>
        _pairs.GetValueOrDefault(key);

    public IReadOnlyDictionary<string, string?> GetVariables() => _pairs;

    public void SetEnvironmentVariable(string key, string? value)
    {
        if (_pairs.ContainsKey(key))
        {
            _pairs.TryRemove(key, out string? _);
        }

        _pairs.TryAdd(key, value);
    }
}