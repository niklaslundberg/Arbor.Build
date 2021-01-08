using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class MSBuildVerbosityLevel
    {
        private MSBuildVerbosityLevel(string level) => Level = level;

        public string Level { get; }

        public static readonly MSBuildVerbosityLevel Normal = new MSBuildVerbosityLevel("normal");

        public static readonly MSBuildVerbosityLevel Detailed = new MSBuildVerbosityLevel("detailed");

        public static readonly MSBuildVerbosityLevel Minimal = new MSBuildVerbosityLevel("minimal");

        public static readonly MSBuildVerbosityLevel Quiet = new MSBuildVerbosityLevel("quiet");

        public static readonly MSBuildVerbosityLevel Default = Quiet;

        public static IEnumerable<MSBuildVerbosityLevel> AllValues
        {
            get
            {
                yield return Normal;
                yield return Detailed;
                yield return Minimal;
                yield return Quiet;
            }
        }

        public static MSBuildVerbosityLevel TryParse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Default;
            }

            MSBuildVerbosityLevel? found =
                AllValues.SingleOrDefault(
                    level => level.Level.Equals(value, StringComparison.OrdinalIgnoreCase));

            if (found is null)
            {
                return Default;
            }

            return found;
        }

        public override string ToString() => Level;
    }
}
