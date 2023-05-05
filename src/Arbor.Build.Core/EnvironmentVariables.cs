using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core;

public class EnvironmentVariables : IEnvironmentVariables
{
    public static readonly EnvironmentVariables Empty = new EnvironmentVariables();

    private readonly ConcurrentDictionary<string, string?> _pairs =
        new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public string? GetEnvironmentVariable(string key) =>
        _pairs.TryGetValue(key, out string? value)
            ? value
            : default;

    public ImmutableDictionary<string, string?> GetVariables() => _pairs.ToImmutableDictionary();

    public void SetEnvironmentVariable(string key, string? value)
    {
        if (_pairs.ContainsKey(key))
        {
            _pairs.TryRemove(key, out string? _);
        }

        _pairs.TryAdd(key, value);
    }
}