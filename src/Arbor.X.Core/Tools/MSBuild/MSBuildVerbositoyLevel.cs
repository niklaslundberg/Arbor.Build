using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.Tools.MSBuild
{
    public sealed class MSBuildVerbositoyLevel
    {
        readonly string _level;

        MSBuildVerbositoyLevel(string level)
        {
            _level = level;
        }

        public static MSBuildVerbositoyLevel Normal
        {
            get { return new MSBuildVerbositoyLevel("normal"); }
        }

        public static MSBuildVerbositoyLevel Detailed
        {
            get { return new MSBuildVerbositoyLevel("detailed"); }
        }

        public static MSBuildVerbositoyLevel Minimal
        {
            get { return new MSBuildVerbositoyLevel("minimal"); }
        }

        public static MSBuildVerbositoyLevel Quiet
        {
            get { return new MSBuildVerbositoyLevel("quiet"); }
        }

        public static MSBuildVerbositoyLevel Default
        {
            get { return Normal; }
        }

        public string Level
        {
            get { return _level; }
        }

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
            return _level;
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