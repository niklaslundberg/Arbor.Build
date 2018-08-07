using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Defensive.Collections;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.KVConfiguration.UserConfiguration;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Parsing;
using Arbor.X.Core.Tools;
using Autofac;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core
{
    public class BuildApplication
    {
        private readonly ILogger _logger;
        private CancellationToken _cancellationToken;
        private IContainer _container;

        public BuildApplication(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExitCode> RunAsync(string[] args)
        {
            MultiSourceKeyValueConfiguration multiSourceKeyValueConfiguration = KeyValueConfigurationManager
                .Add(new UserConfiguration())
                .Add(new EnvironmentVariableKeyValueConfigurationSource())
                .Build();

            StaticKeyValueConfigurationManager.Initialize(multiSourceKeyValueConfiguration);

            bool debugLoggerEnabled = false;

            string sourceDir = null;

            if (DebugHelper.IsDebugging)
            {
                sourceDir = await StartWithDebuggerAsync(args).ConfigureAwait(false);
            }

            _container = await BuildBootstrapper.StartAsync(_logger, sourceDir).ConfigureAwait(false);

            _logger.Information("Using logger '{Type}'", _logger.GetType());

            _cancellationToken = CancellationToken.None;

            ExitCode exitCode;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                ExitCode systemToolsResult = await RunSystemToolsAsync().ConfigureAwait(false);

                if (!systemToolsResult.IsSuccess)
                {
                    const string ToolsMessage = "All system tools did not succeed";

                    _logger.Error(ToolsMessage);

                    exitCode = systemToolsResult;
                }
                else
                {
                    exitCode = ExitCode.Success;
                    _logger.Information("All tools succeeded");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running builds tools");
                exitCode = ExitCode.Failure;
            }

            stopwatch.Stop();

            _logger.Information("Arbor.X.Build total elapsed time in seconds: {TotalSeconds:F}",
                stopwatch.Elapsed.TotalSeconds);

            ParseResult<int> exitDelayInMilliseconds =
                Environment.GetEnvironmentVariable(WellKnownVariables.BuildApplicationExitDelayInMilliseconds)
                    .TryParseInt32(50);

            if (exitDelayInMilliseconds > 0)
            {
                _logger.Debug(
                    "Delaying build application exit with {ExitDelayInMilliseconds} milliseconds specified in '{BuildApplicationExitDelayInMilliseconds}'",
                    exitDelayInMilliseconds,
                    WellKnownVariables.BuildApplicationExitDelayInMilliseconds);

                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds), _cancellationToken)
                    .ConfigureAwait(false);
            }

            if (Debugger.IsAttached)
            {
                WriteDebug($"Exiting build application with exit code {exitCode}");

                if (!debugLoggerEnabled)
                {
                    Debugger.Break();
                }
            }

            return exitCode;
        }

        private static string BuildResults(IEnumerable<ToolResult> toolResults)
        {
            const string NotRun = "Not run";
            const string Succeeded = "Succeeded";
            const string Failed = "Failed";

            string displayTable = toolResults.Select(
                    result =>
                        new Dictionary<string, string>
                        {
                            {
                                "Tool",
                                result.ToolWithPriority.Tool.Name()
                            },
                            {
                                "Result",
                                result.ResultType.WasRun
                                    ? (result.ResultType.IsSuccess ? Succeeded : Failed)
                                    : NotRun
                            },
                            {
                                "Execution time",
                                result.ExecutionTime == default
                                    ? "N/A"
                                    : ((int)result.ExecutionTime.TotalMilliseconds).ToString("D") + " ms"
                            },
                            {
                                "Message",
                                result.Message
                            }
                        })
                .DisplayAsTable();

            return displayTable;
        }

        private async Task<string> StartWithDebuggerAsync([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            string baseDir = VcsPathHelper.FindVcsRootPath(AppDomain.CurrentDomain.BaseDirectory);

            const string tempPath = @"C:\Temp\arbor.x";

            var tempDirectory = new DirectoryInfo(Path.Combine(
                tempPath,
                "D",
                DateTime.UtcNow.ToFileTimeUtc().ToString()));

            tempDirectory.EnsureExists();

            WriteDebug($"Using temp directory '{tempDirectory}'");

            await DirectoryCopy.CopyAsync(
                baseDir,
                tempDirectory.FullName,
                pathLookupSpecificationOption: DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(
                    new[] { "paket-files" }),
                rootDir: baseDir).ConfigureAwait(false);

            WriteDebug("Starting with debugger attached");

            return tempDirectory.FullName;
        }

        private void WriteDebug(string message)
        {
            Debug.WriteLine(message);
            _logger.Debug("{Message}", message);
        }

        private async Task<ExitCode> RunSystemToolsAsync()
        {
            List<IVariable> buildVariables = (await GetBuildVariablesAsync().ConfigureAwait(false)).ToList();

            string variableAsTable = WellKnownVariables.AllVariables
                .OrderBy(item => item.InvariantName)
                .Select(
                    variable => new Dictionary<string, string>
                    {
                        { "Name", variable.InvariantName },
                        { "Description", variable.Description },
                        { "Default value", variable.DefaultValue }
                    })
                .DisplayAsTable();

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowAvailableVariablesEnabled, true))
            {
                _logger.Information("{NewLine}Available wellknown variables: {NewLine1}{NewLine2}{VariableAsTable}",
                    Environment.NewLine,
                    Environment.NewLine,
                    Environment.NewLine,
                    variableAsTable);
            }

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowDefinedVariablesEnabled, true))
            {
                _logger.Information("{NewLine}Defined build variables: [{Count}] {NewLine1}{NewLine2}{V}",
                    Environment.NewLine,
                    buildVariables.Count,
                    Environment.NewLine,
                    Environment.NewLine,
                    buildVariables.OrderBy(variable => variable.Key).Print());
            }

            IReadOnlyCollection<ToolWithPriority> toolWithPriorities = ToolFinder.GetTools(_container, _logger);

            LogTools(toolWithPriorities);

            int result = 0;

            var toolResults = new List<ToolResult>();

            foreach (ToolWithPriority toolWithPriority in toolWithPriorities)
            {
                if (result != 0)
                {
                    if (!toolWithPriority.RunAlways)
                    {
                        toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.NotRun));
                        continue;
                    }
                }

                const int boxLength = 50;

                const char boxCharacter = '#';
                string boxLine = new string(boxCharacter, boxLength);

                string message = string.Format(
                    "{0}{1}{2}{1}{2} Running tool {3}{1}{2}{1}{0}",
                    boxLine,
                    Environment.NewLine,
                    boxCharacter,
                    toolWithPriority);

                _logger.Information("{Message}", message);

                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    ExitCode toolResult =
                        await toolWithPriority.Tool.ExecuteAsync(_logger, buildVariables, _cancellationToken)
                            .ConfigureAwait(false);

                    stopwatch.Stop();

                    if (toolResult.IsSuccess)
                    {
                        _logger.Information("The tool {ToolWithPriority} succeeded with exit code {ToolResult}",
                            toolWithPriority,
                            toolResult);

                        toolResults.Add(new ToolResult(
                            toolWithPriority,
                            ToolResultType.Succeeded,
                            executionTime: stopwatch.Elapsed));
                    }
                    else
                    {
                        _logger.Error("The tool {ToolWithPriority} failed with exit code {ToolResult}",
                            toolWithPriority,
                            toolResult);

                        result = toolResult.Result;

                        toolResults.Add(
                            new ToolResult(
                                toolWithPriority,
                                ToolResultType.Failed,
                                $"failed with exit code {toolResult}",
                                stopwatch.Elapsed));
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    toolResults.Add(new ToolResult(
                        toolWithPriority,
                        ToolResultType.Failed,
                        $"threw {ex.GetType().Name}",
                        stopwatch.Elapsed));
                    _logger.Error(ex, "The tool {ToolWithPriority} failed with exception", toolWithPriority);
                    result = 1;
                }
            }

            string resultTable = BuildResults(toolResults);

            string toolMessage = Environment.NewLine + new string('.', 100) + Environment.NewLine +
                                 Environment.NewLine +
                                 resultTable;

            _logger.Information("{ToolResults}", toolMessage);

            if (result != 0)
            {
                return ExitCode.Failure;
            }

            return ExitCode.Success;
        }

        private void LogTools(IReadOnlyCollection<ToolWithPriority> toolWithPriorities)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.Append("Running tools: [").Append(toolWithPriorities.Count).AppendLine("]");
            sb.AppendLine();

            sb.AppendLine(toolWithPriorities.Select(tool =>
                    new Dictionary<string, string>
                    {
                        {
                            "Tool", tool.Tool.Name()
                        },
                        {
                            "Priority", tool.Priority.ToString(CultureInfo.InvariantCulture)
                        },
                        {
                            "Run always", tool.RunAlways ? "Run always" : string.Empty
                        }
                    })
                .DisplayAsTable());

            _logger.Information("{Tools}", sb.ToString());
        }

        private async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync()
        {
            var buildVariables = new List<IVariable>(500);

            if (
                Environment.GetEnvironmentVariable(WellKnownVariables.VariableFileSourceEnabled)
                    .TryParseBool(false))
            {
                _logger.Information(
                    "The environment variable {VariableFileSourceEnabled} is set to true, using file source to set environment variables",
                    WellKnownVariables.VariableFileSourceEnabled);

                var files = new List<string>
                    { "arborx_environmentvariables.json", "arborx_environmentvariables.json.user" };

                foreach (string configFile in files)
                {
                   ImmutableArray<KeyValue> variables = EnvironmentVariableHelper.GetBuildVariablesFromFile(_logger, configFile);

                    if (variables.IsDefaultOrEmpty)
                    {
                        _logger.Warning(
                            "Could not set environment variables from file, set variable '{Key}' to false to disabled", WellKnownVariables.VariableFileSourceEnabled);
                    }
                }
            }
            else
            {
                _logger.Debug(
                    "The environment variable {VariableFileSourceEnabled} is not set or false, skipping file source to set environment variables",
                    WellKnownVariables.VariableFileSourceEnabled);
            }

            IEnumerable<IVariable> result = CheckEnvironmentLinesInVariables();

            buildVariables.AddRange(result);

            //buildVariables.AddRange(
            //    EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger, buildVariables));

            IReadOnlyCollection<IVariableProvider> providers = _container.Resolve<IEnumerable<IVariableProvider>>()
                .OrderBy(provider => provider.Order)
                .ToReadOnlyCollection();

            string displayAsTable =
                providers.Select(item => new Dictionary<string, string> { { "Provider", item.GetType().Name } })
                    .DisplayAsTable();

            string variablesMessage =
                $"{Environment.NewLine}Available variable providers: [{providers.Count}]{Environment.NewLine}{Environment.NewLine}{displayAsTable}{Environment.NewLine}";

            _logger.Verbose("{Variables}", variablesMessage);

            foreach (IVariableProvider provider in providers)
            {
                IEnumerable<IVariable> newVariables =
                    await provider.GetBuildVariablesAsync(_logger, buildVariables, _cancellationToken)
                        .ConfigureAwait(false);

                foreach (IVariable var in newVariables)
                {
                    if (buildVariables.HasKey(var.Key))
                    {
                        IVariable existing = buildVariables.Single(bv => bv.Key.Equals(var.Key));

                        if (string.IsNullOrWhiteSpace(buildVariables.GetVariableValueOrDefault(var.Key, string.Empty)))
                        {
                            if (string.IsNullOrWhiteSpace(var.Value))
                            {
                                _logger.Warning(
                                    "The build variable {Key} already exists with empty value, new value is also empty",
                                    var.Key);
                                continue;
                            }

                            _logger.Warning(
                                "The build variable {Key} already exists with empty value, using new value '{Value}'",
                                var.Key,
                                var.Value);

                            buildVariables.Remove(existing);
                        }
                        else
                        {
                            if (existing.Value.Equals(var.Value))
                            {
                                continue;
                            }

                            bool variableOverrideEnabled = buildVariables.GetBooleanByKey(
                                WellKnownVariables.VariableOverrideEnabled,
                                true);

                            if (variableOverrideEnabled)
                            {
                                buildVariables.Remove(existing);

                                _logger.Information(
                                    "Flag '{VariableOverrideEnabled}' is set to true, existing variable with key '{Key}' and value '{Value}', replacing the value with '{Value1}'",
                                    WellKnownVariables.VariableOverrideEnabled,
                                    existing.Key,
                                    existing.Value,
                                    var.Value);
                            }
                            else
                            {
                                _logger.Warning(
                                    "The build variable '{Key}' already exists with value '{Value}'. To override variables, set flag '{VariableOverrideEnabled}' to true",
                                    var.Key,
                                    var.Value,
                                    WellKnownVariables.VariableOverrideEnabled);
                                continue;
                            }
                        }
                    }

                    buildVariables.Add(var);
                }
            }

            AddCompatibilityVariables(buildVariables);

            List<IVariable> sorted = buildVariables
                .OrderBy(variable => variable.Key)
                .ToList();

            return sorted;
        }

        private void AddCompatibilityVariables(List<IVariable> buildVariables)
        {
            IVariable[] buildVariableArray = buildVariables.ToArray();

            var alreadyDefined = new List<Dictionary<string, string>>();
            var compatibilities = new List<Dictionary<string, string>>();

            foreach (IVariable buildVariable in buildVariableArray)
            {
                if (!buildVariable.Key.StartsWith("Arbor.X", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                string compatibilityName = buildVariable.Key.Replace(".", "_");

                if (
                    buildVariables.Any(
                        bv => bv.Key.Equals(compatibilityName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    alreadyDefined.Add(new Dictionary<string, string>
                    {
                        { "Name", buildVariable.Key },
                        { "Value", buildVariable.Value }
                    });
                }
                else
                {
                    compatibilities.Add(new Dictionary<string, string>
                    {
                        { "Name", buildVariable.Key },
                        { "Compatibility name", compatibilityName },
                        { "Value", buildVariable.Value }
                    });

                    buildVariables.Add(new BuildVariable(compatibilityName, buildVariable.Value));
                }
            }

            if (alreadyDefined.Count > 0)
            {
                string alreadyDefinedMessage =
                    $"{Environment.NewLine}Compatibility build variables alread defined {Environment.NewLine}{Environment.NewLine}{alreadyDefined.DisplayAsTable()}{Environment.NewLine}";

                _logger.Warning("{AlreadyDefined}", alreadyDefinedMessage);
            }

            if (compatibilities.Count > 0)
            {
                string compatibility =
                    $"{Environment.NewLine}Compatibility build variables added {Environment.NewLine}{Environment.NewLine}{compatibilities.DisplayAsTable()}{Environment.NewLine}";

                _logger.Verbose("{CompatibilityVariables}", compatibility);
            }

            IVariable arborXBranchName =
                buildVariables.SingleOrDefault(
                    var => var.Key.Equals(WellKnownVariables.BranchName, StringComparison.InvariantCultureIgnoreCase));

            if (arborXBranchName != null && !string.IsNullOrWhiteSpace(arborXBranchName.Value))
            {
                const string BranchKey = "branch";
                const string BranchNameKey = "branchName";

                if (!buildVariables.Any(var => var.Key.Equals(BranchKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.Verbose(
                        "Build variable with key '{BranchKey}' was not defined, using value from variable key {Key} ('{Value}')",
                        BranchKey,
                        arborXBranchName.Key,
                        arborXBranchName.Value);
                    buildVariables.Add(new BuildVariable(BranchKey, arborXBranchName.Value));
                }

                if (
                    !buildVariables.Any(
                        var => var.Key.Equals(BranchNameKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.Verbose(
                        "Build variable with key '{BranchNameKey}' was not defined, using value from variable key {Key} ('{Value}')",
                        BranchNameKey,
                        arborXBranchName.Key,
                        arborXBranchName.Value);
                    buildVariables.Add(new BuildVariable(BranchNameKey, arborXBranchName.Value));
                }
            }
        }

        private ImmutableArray<BuildVariable> CheckEnvironmentLinesInVariables()
        {
            var variables = new Dictionary<string, string>();

            List<KeyValuePair<string, string>> newLines =
                variables.Where(item => item.Value.Contains(Environment.NewLine)).ToList();

            if (newLines.Count > 0)
            {
                var variablesWithNewLinesBuilder = new StringBuilder();

                variablesWithNewLinesBuilder.AppendLine("Variables containing new lines: ");

                foreach (KeyValuePair<string, string> keyValuePair in newLines)
                {
                    variablesWithNewLinesBuilder.Append("Key ").Append(keyValuePair.Key).AppendLine(": ");
                    variablesWithNewLinesBuilder.Append("'").Append(keyValuePair.Value).AppendLine("'");
                }

                _logger.Error("{Variables}", variablesWithNewLinesBuilder.ToString());

                throw new InvalidOperationException(variablesWithNewLinesBuilder.ToString());
            }

            return variables.Select(item => new BuildVariable(item.Key, item.Value)).ToImmutableArray();
        }

    }
}
