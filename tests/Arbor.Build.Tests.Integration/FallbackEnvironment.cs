using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arbor.Build.Core.BuildVariables;

#nullable enable

namespace Arbor.Build.Tests.Integration
{
    public class FallbackEnvironment : IEnvironmentVariables
    {
        readonly IEnvironmentVariables _environmentVariables;
        readonly IEnvironmentVariables _defaultEnvironmentVariables;

        public FallbackEnvironment(IEnvironmentVariables environmentVariables, IEnvironmentVariables defaultEnvironmentVariables)
        {
            _environmentVariables = environmentVariables;
            _defaultEnvironmentVariables = defaultEnvironmentVariables;
        }

        public string? GetEnvironmentVariable(string key) => _environmentVariables.GetEnvironmentVariable(key) ?? _defaultEnvironmentVariables.GetEnvironmentVariable(key);

        public ImmutableArray<KeyValuePair<string, string?>> GetVariables() => _environmentVariables.GetVariables().Concat(_defaultEnvironmentVariables.GetVariables()).ToImmutableArray();

        public void SetEnvironmentVariable(string key, string? value) => _environmentVariables.SetEnvironmentVariable(key, value);
    }
}