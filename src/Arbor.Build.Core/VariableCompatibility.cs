using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core
{
    public static class VariableCompatibility
    {
        public static void AddCompatibilityVariables(this List<IVariable> buildVariables, ILogger _logger)
        {
            bool _verboseEnabled = _logger.IsEnabled(LogEventLevel.Verbose);

            var replacements = new Dictionary<string, string> { ["Arbor.X"] = "Arbor.Build", ["."] = "_" };


            var alreadyDefined = new List<Dictionary<string, string?>>();
            var compatibilities = new List<Dictionary<string, string?>>();

            foreach (var replacement in replacements)
            {
                IVariable[] buildVariableArray = buildVariables.ToArray();

                foreach (IVariable buildVariable in buildVariableArray)
                {
                    if (!buildVariable.Key.StartsWithAny(new[] { ArborConstants.ArborBuild, ArborConstants.ArborX },
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

                    _logger.Debug("{AlreadyDefined}", alreadyDefinedMessage);
                }

                if (compatibilities.Count > 0 && _verboseEnabled)
                {
                    string compatibility =
                        $"{Environment.NewLine}Compatibility build variables added {Environment.NewLine}{Environment.NewLine}{compatibilities.DisplayAsTable()}{Environment.NewLine}";

                    _logger.Verbose("{CompatibilityVariables}", compatibility);
                }

                IVariable arborXBranchName =
                    buildVariables.SingleOrDefault(
                        var => var.Key.Equals(WellKnownVariables.BranchName, StringComparison.OrdinalIgnoreCase));

                if (arborXBranchName != null && !string.IsNullOrWhiteSpace(arborXBranchName.Value))
                {
                    const string BranchKey = "branch";
                    const string BranchNameKey = "branchName";

                    if (!buildVariables.Any(var => var.Key.Equals(BranchKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (_verboseEnabled)
                        {
                            _logger.Verbose(
                                "Build variable with key '{BranchKey}' was not defined, using value from variable key {Key} ('{Value}')",
                                BranchKey,
                                arborXBranchName.Key,
                                arborXBranchName.Value);
                        }

                        buildVariables.Add(new BuildVariable(BranchKey, arborXBranchName.Value));
                    }

                    if (
                        !buildVariables.Any(
                            var => var.Key.Equals(BranchNameKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (_verboseEnabled)
                        {
                            _logger.Verbose(
                                "Build variable with key '{BranchNameKey}' was not defined, using value from variable key {Key} ('{Value}')",
                                BranchNameKey,
                                arborXBranchName.Key,
                                arborXBranchName.Value);
                        }

                        buildVariables.Add(new BuildVariable(BranchNameKey, arborXBranchName.Value));
                    }
                }
            }
        }
    }
}