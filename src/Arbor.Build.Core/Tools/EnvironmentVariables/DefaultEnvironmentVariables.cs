using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.Tools.EnvironmentVariables;

public class DefaultEnvironmentVariables : IEnvironmentVariables
{
    public string? GetEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(key);

    public ImmutableDictionary<string, string?> GetVariables() =>
        Environment.GetEnvironmentVariables()
            .OfType<DictionaryEntry>()
            .Select(pair =>
                (Key: pair.Key as string, Value: pair.Value as string))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new KeyValuePair<string, string?>(pair.Key!, pair.Value))
            .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public void SetEnvironmentVariable(string key, string? value) => Environment.SetEnvironmentVariable(key, value);
}