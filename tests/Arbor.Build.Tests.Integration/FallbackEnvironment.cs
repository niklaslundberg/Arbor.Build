using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Tests.Integration;

public class FallbackEnvironment(
    IEnvironmentVariables environmentVariables,
    IEnvironmentVariables defaultEnvironmentVariables)
    : IEnvironmentVariables
{
    public string? GetEnvironmentVariable(string key) => environmentVariables.GetEnvironmentVariable(key) ??
                                                         defaultEnvironmentVariables.GetEnvironmentVariable(key);

    public IReadOnlyDictionary<string, string?> GetVariables()
    {
        var keyValuePairs = environmentVariables.GetVariables();

        var fallbackVariables = defaultEnvironmentVariables.GetVariables()
            .Where(pair => !keyValuePairs.ContainsKey(pair.Key));

        return keyValuePairs.Concat(fallbackVariables).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public void SetEnvironmentVariable(string key, string? value) =>
        environmentVariables.SetEnvironmentVariable(key, value);
}