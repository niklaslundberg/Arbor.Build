using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.Tools.MSBuild
{
    public sealed class MSBuildVerbositoyLevel
    {
        private MSBuildVerbositoyLevel(string level)
        {
            Level = level;
        }

        public static MSBuildVerbositoyLevel Normal => new MSBuildVerbositoyLevel("normal");

        public static MSBuildVerbositoyLevel Detailed => new MSBuildVerbositoyLevel("detailed");

        public static MSBuildVerbositoyLevel Minimal => new MSBuildVerbositoyLevel("minimal");

        public static MSBuildVerbositoyLevel Quiet => new MSBuildVerbositoyLevel("quiet");

        public static MSBuildVerbositoyLevel Default => Normal;

        public string Level { get; }

        public static IEnumerable<MSBuildVerbositoyLevel> AllValues
        {
            get
            {
                yield return Normal;
                yield return Detailed;
                yield return Minimal;
                yield return Quiet;
            }
        }

        public override string ToString()
        {
            return Level;
        }

        public static MSBuildVerbositoyLevel TryParse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Default;
            }

            MSBuildVerbositoyLevel found =
                AllValues.SingleOrDefault(
                    level => level.Level.Equals(value, StringComparison.InvariantCultureIgnoreCase));

            if (found == null)
            {
                return Default;
            }

            return found;
        }
    }
}
