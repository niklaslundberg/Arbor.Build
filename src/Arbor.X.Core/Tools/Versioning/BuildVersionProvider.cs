using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Alphaleonis.Win32.Filesystem;

using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.Schema;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class BuildVersionProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, string>> variables =
                GetVersionVariables(buildVariables);

            List<EnvironmentVariable> environmentVariables = variables
                .Select(item => new EnvironmentVariable(item.Key, item.Value))
                .ToList();

            return Task.FromResult<IEnumerable<IVariable>>(environmentVariables);
        }

        IEnumerable<KeyValuePair<string, string>> GetVersionVariables(
            IReadOnlyCollection<IVariable> buildVariables)
        {

            var environmentVariables =
                buildVariables.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList();

            int major =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMajor)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int minor =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionMinor)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int patch =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionPatch)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;
            int build =
                environmentVariables.Where(item => item.Key == WellKnownVariables.VersionBuild)
                    .Select(item => (int?) int.Parse(item.Value))
                    .SingleOrDefault() ?? -1;

            string sourceRoot = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var versionFileName = Path.Combine(sourceRoot, "version.json");

            if (File.Exists(versionFileName))
            {
                IReadOnlyCollection<KeyValueConfigurationItem> keyValueConfigurationItems = new JsonFileReader(versionFileName).ReadConfiguration();

                var jsonKeyValueConfiguration = new JsonKeyValueConfiguration(keyValueConfigurationItems);

                var majorKey = "major"; //TODO defined major key

                var minorKey = "minor"; //TODO defined minor key

                var patchKey = "patch"; //TODO defined patch key

                var required = new Dictionary<string, string>
                                   {
                                       { majorKey, jsonKeyValueConfiguration.ValueOrDefault(majorKey)},
                                       { minorKey, jsonKeyValueConfiguration.ValueOrDefault(minorKey)},
                                       { patchKey, jsonKeyValueConfiguration.ValueOrDefault(patchKey)}
                                   };

                if (required.All(ValidateVersionNumber))
                {
                    major = required[majorKey].TryParseInt32().Value;
                    minor = required[minorKey].TryParseInt32().Value;
                    patch = required[patchKey].TryParseInt32().Value;
                }
            }

            if (major < 0)
            {
                major = 0;
            }

            if (minor < 0)
            {
                minor = 0;
            }

            if (patch < 0)
            {
                patch = 0;
            }

            if (major == 0 && minor == 0 && patch == 0)
            {
                minor = 1;
            }

            Version netAssemblyVersion = new Version(major, minor, 0, 0);
            Version fullVersion = new Version(major, minor, patch, build);
            string fullVersionValue = fullVersion.ToString(fieldCount: 4);
            Version netAssemblyFileVersion = new Version(major, minor, patch, build);

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

        private static bool ValidateVersionNumber(KeyValuePair<string, string> s)
        {
            if (string.IsNullOrWhiteSpace(s.Value))
            {
                return false;
            }

            int parsed;

            if (!int.TryParse(s.Value, out parsed) || parsed < 0)
            {
                return false;
            }

            return true;
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}
