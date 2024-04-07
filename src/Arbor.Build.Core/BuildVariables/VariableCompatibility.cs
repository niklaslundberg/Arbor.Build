using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.GenericExtensions;
using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core.BuildVariables;

public static class VariableCompatibility
{
    public static void AddCompatibilityVariables(this List<IVariable> buildVariables, ILogger logger)
    {
        bool verboseEnabled = logger.IsEnabled(LogEventLevel.Verbose);

        var replacements = new Dictionary<string, string> { ["."] = "_" };


        var alreadyDefined = new List<Dictionary<string, string?>>();
        var compatibilities = new List<Dictionary<string, string?>>();

        foreach (var replacement in replacements)
        {
            IVariable[] buildVariableArray = buildVariables.ToArray();

            foreach (IVariable buildVariable in buildVariableArray)
            {
                if (!buildVariable.Key.StartsWith(ArborConstants.ArborBuild,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string compatibilityName = buildVariable.Key
                    .Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);

                if (
                    buildVariables.Any(
                        bv => bv.Key.Equals(compatibilityName, StringComparison.OrdinalIgnoreCase)))
                {
                    alreadyDefined.Add(new Dictionary<string, string?>
                    {
                        { "Name", buildVariable.Key },
                        { "Value", buildVariable.Key.GetDisplayValue(buildVariable.Value) }
                    });
                }
                else
                {
                    compatibilities.Add(new Dictionary<string, string?>
                    {
                        { "Name", buildVariable.Key },
                        { "Compatibility name", compatibilityName },
                        { "Value", buildVariable.Key.GetDisplayValue(buildVariable.Value) }
                    });

                    buildVariables.Add(new BuildVariable(compatibilityName, buildVariable.Value));
                }
            }

            if (alreadyDefined.Count > 0)
            {
                string alreadyDefinedMessage =
                    $"{Environment.NewLine}Compatibility build variables already defined {Environment.NewLine}{Environment.NewLine}{alreadyDefined.DisplayAsTable()}{Environment.NewLine}";

                logger.Debug("{AlreadyDefined}", alreadyDefinedMessage);
            }

            if (compatibilities.Count > 0 && verboseEnabled)
            {
                string compatibility =
                    $"{Environment.NewLine}Compatibility build variables added {Environment.NewLine}{Environment.NewLine}{compatibilities.DisplayAsTable()}{Environment.NewLine}";

                logger.Verbose("{CompatibilityVariables}", compatibility);
            }

            IVariable? arborXBranchName =
                buildVariables.SingleOrDefault(
                    var => var.Key.Equals(WellKnownVariables.BranchName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(arborXBranchName?.Value))
            {
                const string branchKey = "branch";
                const string branchNameKey = "branchName";

                if (!buildVariables.Any(var => var.Key.Equals(branchKey, StringComparison.OrdinalIgnoreCase)))
                {
                    if (verboseEnabled)
                    {
                        logger.Verbose(
                            "Build variable with key '{BranchKey}' was not defined, using value from variable key {Key} ('{Value}')",
                            branchKey,
                            arborXBranchName.Key,
                            arborXBranchName.Value);
                    }

                    buildVariables.Add(new BuildVariable(branchKey, arborXBranchName.Value));
                }

                if (
                    !buildVariables.Any(
                        var => var.Key.Equals(branchNameKey, StringComparison.OrdinalIgnoreCase)))
                {
                    if (verboseEnabled)
                    {
                        logger.Verbose(
                            "Build variable with key '{BranchNameKey}' was not defined, using value from variable key {Key} ('{Value}')",
                            branchNameKey,
                            arborXBranchName.Key,
                            arborXBranchName.Value);
                    }

                    buildVariables.Add(new BuildVariable(branchNameKey, arborXBranchName.Value));
                }
            }
        }
    }
}