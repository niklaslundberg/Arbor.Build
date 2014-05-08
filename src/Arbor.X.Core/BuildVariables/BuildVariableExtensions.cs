using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Arbor.X.Core.BuildVariables
{
    public static class BuildVariableExtensions
    {
        public static bool HasKey(this IReadOnlyCollection<IVariable> buildVariables, string key)
        {
            return buildVariables.Any(
                bv => bv.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static IVariable GetVariable(this IReadOnlyCollection<IVariable> buildVariables, string key)
        {
            return buildVariables.Single(
                bv => bv.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static string GetVariableValueOrDefault(this IReadOnlyCollection<IVariable> buildVariables, string key,
            string defaultValue)
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }
            return buildVariables.GetVariable(key).Value;
        }

        public static bool GetBooleanByKey(this IReadOnlyCollection<IVariable> buildVariables, string key,
            bool defaultValue = false)
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }

            string value = buildVariables.GetVariableValueOrDefault(key, defaultValue.ToString());

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            bool parsed;

            if (!bool.TryParse(value, out parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static int GetInt32ByKey(this IReadOnlyCollection<IVariable> buildVariables, string key,
            int defaultValue = default(int), int? minValue = null)
        {
            int? returnValue = null;

            if (buildVariables.HasKey(key))
            {
                string value = buildVariables.GetVariableValueOrDefault(key,
                    defaultValue.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrWhiteSpace(value))
                {
                    int parsed;
                    if (int.TryParse(value, out parsed))
                    {
                        returnValue = parsed;
                    }
                }
            }

            if (!returnValue.HasValue)
            {
                returnValue = defaultValue;
            }

            if (minValue.HasValue)
            {
                if (returnValue < minValue)
                {
                    returnValue = minValue;
                }
            }

            return returnValue.Value;
        }

        public static long GetInt64ByKey(this IReadOnlyCollection<IVariable> buildVariables, string key,
            long defaultValue = default(long))
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }

            string value = buildVariables.GetVariableValueOrDefault(key,
                defaultValue.ToString(CultureInfo.InvariantCulture));

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            long parsed;

            if (!long.TryParse(value, out parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static bool GetValueOrDefault(this IVariable variable, bool defaultValue = false)
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            bool parsed;

            if (!bool.TryParse(variable.Value, out parsed))
            {
                return defaultValue;
            }
            return parsed;
        }

        public static int GetValueOrDefault(this IVariable variable, int defaultValue = default(int))
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            int parsed;

            if (!int.TryParse(variable.Value, out parsed))
            {
                return defaultValue;
            }
            return parsed;
        }

        public static long GetValueOrDefault(this IVariable variable, long defaultValue = default(long))
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            long parsed;

            if (!long.TryParse(variable.Value, out parsed))
            {
                return defaultValue;
            }
            return parsed;
        }
    }
}