using System;
using System.Collections.Immutable;
using System.Linq;
using Arbor.Build.Core.BuildVariables;

#nullable enable

namespace Arbor.Build.Tests.Integration
{
    public class FallbackEnvironment : IEnvironmentVariables
    {
        readonly IEnvironmentVariables _defaultEnvironmentVariables;
        readonly IEnvironmentVariables _environmentVariables;

        public FallbackEnvironment(IEnvironmentVariables environmentVariables,
            IEnvironmentVariables defaultEnvironmentVariables)
        {
            _environmentVariables = environmentVariables;
            _defaultEnvironmentVariables = defaultEnvironmentVariables;
        }

        public string? GetEnvironmentVariable(string key) => _environmentVariables.GetEnvironmentVariable(key) ??
                                                             _defaultEnvironmentVariables.GetEnvironmentVariable(key);

        public ImmutableDictionary<string, string?> GetVariables()
        {
            var keyValuePairs = _environmentVariables.GetVariables();

            var fallbackVariables = _defaultEnvironmentVariables.GetVariables()
                .Where(pair => !keyValuePairs.ContainsKey(pair.Key));

            return keyValuePairs.Concat(fallbackVariables).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public void SetEnvironmentVariable(string key, string? value) =>
            _environmentVariables.SetEnvironmentVariable(key, value);
    }
}