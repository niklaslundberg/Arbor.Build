using System.Collections.Immutable;

namespace Arbor.Build.Core.BuildVariables
{
    public interface IEnvironmentVariables
    {
        public string? GetEnvironmentVariable(string key);

        public ImmutableDictionary<string, string?> GetVariables();

        void SetEnvironmentVariable(string key, string? value);
    }
}