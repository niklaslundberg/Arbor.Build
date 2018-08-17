using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Arbor.Defensive;

namespace Arbor.Build.Core.BuildVariables
{
    public static class BuildVariableExtensions
    {
        public static bool HasKey(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key)
        {
            return buildVariables.Any(
                bv => bv.Key.Equals(
                    key,
                    StringComparison.InvariantCultureIgnoreCase));
        }

        public static IVariable GetVariable(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key)
        {
            return buildVariables.Single(
                bv => bv.Key.Equals(
                    key,
                    StringComparison.InvariantCultureIgnoreCase));
        }

        public static Maybe<IVariable> GetOptionalVariable(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key)
        {
            IVariable variable = buildVariables.SingleOrDefault(
                bv => bv.Key.Equals(
                    key,
                    StringComparison.InvariantCultureIgnoreCase));

            if (variable is null)
            {
                return Maybe<IVariable>.Empty();
            }

            return new Maybe<IVariable>(variable);
        }

        public static string GetVariableValueOrDefault(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key,
            string defaultValue)
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }

            return buildVariables.GetVariable(key).Value;
        }

        public static bool GetBooleanByKey(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key,
            bool defaultValue = false)
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }

            string value = buildVariables.GetVariableValueOrDefault(
                key,
                string.Empty);

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!bool.TryParse(
                value,
                out bool parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static bool? GetOptionalBooleanByKey(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key)
        {
            if (!buildVariables.HasKey(key))
            {
                return default;
            }

            string value = buildVariables.GetVariableValueOrDefault(
                key,
                default);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            if (!bool.TryParse(
                value,
                out bool parsed))
            {
                return default;
            }

            return parsed;
        }

        public static int GetInt32ByKey(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key,
            int defaultValue = default,
            int? minValue = null)
        {
            int? returnValue = null;

            if (buildVariables.HasKey(key))
            {
                string value = buildVariables.GetVariableValueOrDefault(
                    key,
                    defaultValue.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (int.TryParse(value, out int parsed))
                    {
                        returnValue = parsed;
                    }
                }
            }

            if (!returnValue.HasValue)
            {
                returnValue = defaultValue;
            }

            if (returnValue < minValue)
            {
                returnValue = minValue;
            }

            return returnValue.Value;
        }

        public static long GetInt64ByKey(
            this IReadOnlyCollection<IVariable> buildVariables,
            string key,
            long defaultValue = default)
        {
            if (!buildVariables.HasKey(key))
            {
                return defaultValue;
            }

            string value = buildVariables.GetVariableValueOrDefault(
                key,
                defaultValue.ToString(CultureInfo.InvariantCulture));

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!long.TryParse(
                value,
                out long parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static bool GetValueOrDefault(
            this IVariable variable,
            bool defaultValue = false)
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            if (!bool.TryParse(
                variable.Value,
                out bool parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static int GetValueOrDefault(
            this IVariable variable,
            int defaultValue = default)
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            if (!int.TryParse(
                variable.Value,
                out int parsed))
            {
                return defaultValue;
            }

            return parsed;
        }

        public static long GetValueOrDefault(
            this IVariable variable,
            long defaultValue = default)
        {
            if (variable == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrWhiteSpace(variable.Value))
            {
                return defaultValue;
            }

            if (!long.TryParse(
                variable.Value,
                out long parsed))
            {
                return defaultValue;
            }

            return parsed;
        }
    }
}
