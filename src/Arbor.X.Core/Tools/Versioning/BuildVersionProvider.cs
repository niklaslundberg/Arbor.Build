using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.Versioning
{
    public class BuildVersionProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var variables =
                GetVersionVariables(
                    buildVariables.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList());

            var environmentVariables = variables
                .Select(item => new EnvironmentVariable(item.Key, item.Value))
                .ToList();

            return Task.FromResult<IEnumerable<IVariable>>(environmentVariables);
        }

        IEnumerable<KeyValuePair<string, string>> GetVersionVariables(
            IReadOnlyList<KeyValuePair<string, string>> environmentVariables)
        {
            var major =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMajor)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? 0;
            var minor =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMinor)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? 1;
            var patch =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionPatch)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? 0;
            var build =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionBuild)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? 0;

            var netAssemblyVersion = new Version(major, minor, 0, 0);
            var fullVersion = new Version(major, minor, patch, build);
            var fullVersionValue = fullVersion.ToString(fieldCount: 4);
            var netAssemblyFileVersion = new Version(major, minor, patch, build);
            
            yield return
                new KeyValuePair<string, string>(WellKnownVariables.NetAssemblyVersion, netAssemblyVersion.ToString(fieldCount: 4));
            yield return
                new KeyValuePair<string, string>(WellKnownVariables.NetAssemblyFileVersion,
                    netAssemblyFileVersion.ToString(fieldCount: 4));
            yield return new KeyValuePair<string, string>(WellKnownVariables.VersionMajor, major.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(WellKnownVariables.VersionMinor, minor.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(WellKnownVariables.VersionPatch, patch.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>(WellKnownVariables.VersionBuild, build.ToString(CultureInfo.InvariantCulture));
            yield return new KeyValuePair<string, string>("Version", fullVersionValue);
            yield return new KeyValuePair<string, string>(WellKnownVariables.Version, fullVersionValue);
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}