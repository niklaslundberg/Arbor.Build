using System.Collections.Generic;

namespace Arbor.Build.Core.BuildVariables;

public interface IEnvironmentVariables
{
    public string? GetEnvironmentVariable(string key);

    public IReadOnlyDictionary<string, string?> GetVariables();

    void SetEnvironmentVariable(string key, string? value);
}