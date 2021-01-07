using System;
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
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Debugging;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Build.Core.GenericExtensions.Int;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.KVConfiguration.UserConfiguration;
using Arbor.Processing;
using Autofac;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Zio;

namespace Arbor.Build.Core
{
    public sealed class BuildApplication : IDisposable
    {
        private readonly bool _debugEnabled;
        private readonly ILogger _logger;
        private readonly bool _verboseEnabled;
        private CancellationToken _cancellationToken;
        private IContainer? _container;
        private string[] _args;
        private readonly IEnvironmentVariables _environmentVariables;
        private readonly ISpecialFolders _specialFolders;
        private readonly IFileSystem _fileSystem;
        private BuildContext _buildContext = null!;

        public BuildApplication(ILogger? logger, IEnvironmentVariables environmentVariables, ISpecialFolders specialFolders, IFileSystem fileSystem)
        {
            _args = Array.Empty<string>();
            _environmentVariables = environmentVariables;
            _specialFolders = specialFolders;
            _fileSystem = fileSystem;
            _logger = logger ?? Logger.None!;
            _verboseEnabled = _logger.IsEnabled(LogEventLevel.Verbose);
            _debugEnabled = _logger.IsEnabled(LogEventLevel.Debug);
        }

        private static string BuildResultsAsTable(IReadOnlyCollection<ToolResult> toolResults)
        {
            const string NotRun = "Not run";
            const string Succeeded = "Succeeded";
            const string Failed = "Failed";

            static string GetResult(ToolResult toolResult)
            {
                string succeeded = toolResult.ResultType.IsSuccess ? Succeeded : Failed;

                return toolResult.ResultType.WasRun
                    ? succeeded
                    : NotRun;
            }

            static string GetExecutionTime(ToolResult result1)
            {
                return result1.ExecutionTime == default
                    ? "N/A"
                    : ((int)result1.ExecutionTime.TotalMilliseconds).ToString("D",
                        CultureInfo.InvariantCulture) + " ms";
            }

            string displayTable = toolResults.Select(
                    result =>
                        new Dictionary<string, string?>
                        {
                            {"Tool", result.ToolWithPriority.Tool.Name()},
                            {"Result", GetResult(result)},
                            {"Execution time", GetExecutionTime(result)},
                            {"Message", result.Message}
                        })
                .DisplayAsTable();

            return displayTable;
        }

        private async Task<DirectoryEntry?> StartWithDebuggerAsync()
        {
            var baseDir = new DirectoryEntry(_fileSystem, VcsPathHelper.FindVcsRootPath(AppContext.BaseDirectory).ParseAsPath());

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Enter base directory");
                string? baseDirectory = Console.ReadLine();

                if (baseDirectory == "-")
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(baseDirectory))
                {
                    var asFullPath = baseDirectory.ParseAsPath();
                    baseDir = new DirectoryEntry(_fileSystem, asFullPath);

                    Directory.SetCurrentDirectory(_fileSystem.ConvertPathToInternal(asFullPath));
                }
            }

            const string tempPath = @"C:\Work\Arbor.Build";

            var tempDirectory = new DirectoryEntry( _fileSystem, UPath.Combine(
                tempPath.ParseAsPath(),
                "D",
                DateTime.UtcNow.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture)));

            tempDirectory.EnsureExists();

            WriteDebug($"Using temp directory '{tempDirectory}'");

            await DirectoryCopy.CopyAsync(
                baseDir,
                tempDirectory,
                pathLookupSpecificationOption: DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(
                    new[] { "paket-files" }),
                rootDir: baseDir).ConfigureAwait(false);

            WriteDebug("Starting with debugger attached");

            return tempDirectory;
        }

        private void WriteDebug(string message)
        {
            Debug.WriteLine(message);
            _logger.Debug("{Message}", message);
        }

        private async Task<ExitCode> RunSystemToolsAsync(DirectoryEntry? sourceRoot)
        {
            IReadOnlyCollection<IVariable> buildVariables = await GetBuildVariablesAsync(sourceRoot).ConfigureAwait(false);

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowAvailableVariablesEnabled, true))
            {
                string variableAsTable = WellKnownVariables.AllVariables
                    .OrderBy(item => item.InvariantName)
                    .Select(
                        variable => new Dictionary<string, string?>
                        {
                            { "Name", variable.InvariantName },
                            { "Description", variable.Description },
                            { "Default value", variable.DefaultValue }
                        })
                    .DisplayAsTable();

                _logger.Information("{NewLine}Available wellknown variables: {NewLine1}{NewLine2}{VariableAsTable}",
                    Environment.NewLine,
                    Environment.NewLine,
                    Environment.NewLine,
                    variableAsTable);
            }

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowDefinedVariablesEnabled))
            {
                _logger.Information("{NewLine}Defined build variables: [{Count}] {NewLine1}{NewLine2}{V}",
                    Environment.NewLine,
                    buildVariables.Count,
                    Environment.NewLine,
                    Environment.NewLine,
                    buildVariables.OrderBy(variable => variable.Key).Print());
            }

            IReadOnlyCollection<ToolWithPriority> toolWithPriorities = ToolFinder.GetTools(_container!, _logger);

            LogToolsAsTable(toolWithPriorities);

            int result = 0;

            var toolResults = new List<ToolResult>();

            bool testsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.TestsEnabled, true);

            foreach (ToolWithPriority toolWithPriority in toolWithPriorities)
            {
                if (result != 0 && !toolWithPriority.RunAlways)
                {
                    toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.NotRun));
                    continue;
                }

                if (!testsEnabled
                    && toolWithPriority.Tool is ITestRunnerTool)
                {
                    toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.NotRun));
                    continue;
                }

                const int boxLength = 50;

                const char boxCharacter = '#';
                string boxLine = new string(boxCharacter, boxLength);

                string message = string.Format(CultureInfo.InvariantCulture,
                    "{1}{0}{1}{2}{1}{2} Running tool {3}{1}{2}{1}{0}",
                    boxLine,
                    Environment.NewLine,
                    boxCharacter,
                    toolWithPriority);

                _logger.Information("{Message}", message);

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    ExitCode toolResult =
                        await toolWithPriority.Tool.ExecuteAsync(_logger, buildVariables, _args, _cancellationToken)
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

                        result = toolResult.Code;

                        toolResults.Add(
                            new ToolResult(
                                toolWithPriority,
                                ToolResultType.Failed,
                                $"failed with exit code {toolResult}",
                                stopwatch.Elapsed));
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
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

            string resultTable = BuildResultsAsTable(toolResults);

            string toolMessage = Environment.NewLine + new string('.', 100) + Environment.NewLine +
                                 Environment.NewLine +
                                 resultTable;

            _logger.Information("{ToolResults}", toolMessage);

            if (result != 0)
            {
                foreach (var (toolResultValue, reportLogTail) in toolResults
                    .Where(tool => tool.ResultType == ToolResultType.Failed)
                    .Select(toolResult => (Result:toolResult, LogTail:toolResult.ToolWithPriority.Tool as IReportLogTail))
                    .Where(item => item.LogTail is {}))
                {
                    string logTail = string.Join(Environment.NewLine, reportLogTail!.LogTail.AllCurrentItems);
                    _logger.Error("Tool {Tool} failed with log tail {NewLine}{LogTail}", toolResultValue.ToolWithPriority.Tool.Name(), Environment.NewLine, logTail);
                }

                return ExitCode.Failure;
            }

            return ExitCode.Success;
        }

        private void LogToolsAsTable(IReadOnlyCollection<ToolWithPriority> toolWithPriorities)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.Append("Running tools: [").Append(toolWithPriorities.Count).AppendLine("]");
            sb.AppendLine();

            sb.AppendLine(toolWithPriorities.Select(tool =>
                    new Dictionary<string, string?>
                    {
                        { "Tool", tool.Tool.Name() },
                        { "Priority", tool.Priority.ToString(CultureInfo.InvariantCulture) },
                        { "Run always", tool.RunAlways ? "Run always" : string.Empty }
                    })
                .DisplayAsTable());

            _logger.Information("{Tools}", sb.ToString());
        }

        private async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(DirectoryEntry? sourceRoot)
        {
            var buildVariables = new List<IVariable>(500);

            _ = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.VariableFileSourceEnabled)
                .TryParseBool(out bool enabled, defaultValue: true);

            UPath? sourceRootPath = _environmentVariables.GetEnvironmentVariable(WellKnownVariables.SourceRoot)?.ParseAsPath();

            if (sourceRoot is null && sourceRootPath is null)
            {
               string vcsRootPath = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

               if (!string.IsNullOrWhiteSpace(vcsRootPath))
               {
                   sourceRootPath = vcsRootPath.ParseAsPath();
               }
               else
               {
                   _logger.Error("Source root is not set");
                   return ImmutableArray<IVariable>.Empty;
               }
            }

            sourceRoot ??= new DirectoryEntry(_fileSystem, sourceRootPath!.Value);

            _buildContext.SourceRoot = sourceRoot;

            if (enabled)
            {
                _logger.Information(
                    "The environment variable {VariableFileSourceEnabled} is set to true, using file source to set environment variables",
                    WellKnownVariables.VariableFileSourceEnabled);

                var files = new List<string>
                {
                    "arborbuild_environmentvariables.json", "arborbuild_environmentvariables.json.user"
                };

                foreach (string configFile in files)
                {
                    ImmutableArray<KeyValue> variables =
                        EnvironmentVariableHelper.GetBuildVariablesFromFile(_logger, configFile, sourceRoot);

                    if (variables.IsDefaultOrEmpty)
                    {
                        _logger.Warning(
                            "Could not set environment variables from file {File}",
                            configFile);
                    }
                    else
                    {
                        foreach (var variable in variables)
                        {
                            _environmentVariables.SetEnvironmentVariable(variable.Key, variable.Value);
                        }
                    }
                }
            }
            else
            {
                _logger.Debug(
                    "The environment variable {VariableFileSourceEnabled} is not set or false, skipping file source to set environment variables",
                    WellKnownVariables.VariableFileSourceEnabled);
            }

            CheckEnvironmentLinesInVariables(buildVariables);

            buildVariables.AddRange(
                EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger, _environmentVariables, buildVariables));

            IReadOnlyCollection<IVariableProvider> providers = _container!.Resolve<IEnumerable<IVariableProvider>>()
                .OrderBy(provider => provider.Order)
                .ToReadOnlyCollection();

            if (_verboseEnabled)
            {
                string displayAsTable =
                    providers.Select(item => new Dictionary<string, string?> { { "Provider", item.GetType().Name } })
                        .DisplayAsTable();

                string variablesMessage =
                    $"{Environment.NewLine}Available variable providers: [{providers.Count}]{Environment.NewLine}{Environment.NewLine}{displayAsTable}{Environment.NewLine}";

                _logger.Verbose("{Variables}", variablesMessage);
            }

            foreach (IVariableProvider provider in providers)
            {
                if (_verboseEnabled)
                {
                    _logger.Verbose("### Running variable provider {Provider}", provider.GetType().Name);
                }

                ImmutableArray<IVariable> newVariables =
                    await provider.GetBuildVariablesAsync(_logger, buildVariables, _cancellationToken)
                        .ConfigureAwait(false);

                if (_verboseEnabled)
                {
                    string values;
                    if (newVariables.Length > 0)
                    {
                        Dictionary<string, string?>[] providerTable =
                        {
                            newVariables.ToDictionary(s => s.Key, s => s.Value)
                        };
                        values = providerTable.DisplayAsTable();
                    }
                    else
                    {
                        values = "[None]";
                    }

                    _logger.Verbose("Variable provider {Provider} provided variables {Variables}",
                        provider.GetType().Name,
                        values);
                }

                foreach (IVariable variable in newVariables)
                {
                    if (buildVariables.HasKey(variable.Key))
                    {
                        IVariable existing = buildVariables.Single(bv =>
                            bv.Key.Equals(variable.Key, StringComparison.OrdinalIgnoreCase));

                        if (string.IsNullOrWhiteSpace(
                            buildVariables.GetVariableValueOrDefault(variable.Key, string.Empty)))
                        {
                            if (string.IsNullOrWhiteSpace(variable.Value))
                            {
                                _logger.Warning(
                                    "The build variable {Key} already exists with empty value, new value is also empty",
                                    variable.Key);
                                continue;
                            }

                            _logger.Warning(
                                "The build variable {Key} already exists with empty value, using new value '{Value}'",
                                variable.Key,
                                variable.Value);

                            buildVariables.Remove(existing);
                        }
                        else
                        {
                            if (existing.Value is {} existingValue && existingValue.Equals(variable.Value, StringComparison.OrdinalIgnoreCase))
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
                                    existing.Key.GetDisplayValue(existing.Value),
                                    existing.Key.GetDisplayValue(variable.Value));
                            }
                            else
                            {
                                _logger.Warning(
                                    "The build variable '{Key}' already exists with value '{Value}'. To override variables, set flag '{VariableOverrideEnabled}' to true",
                                    variable.Key,
                                    variable.Value,
                                    WellKnownVariables.VariableOverrideEnabled);
                                continue;
                            }
                        }
                    }

                    buildVariables.Add(variable);
                }
            }

            buildVariables.AddCompatibilityVariables(_logger);

            var sorted = buildVariables
                .OrderBy(variable => variable.Key)
                .ToList();

            return sorted;
        }


        private void CheckEnvironmentLinesInVariables(List<IVariable> buildVariables)
        {
            var newLines =
                buildVariables.Where(item => item.Value is {} && item.Value.Contains(Environment.NewLine, StringComparison.Ordinal)).ToList();

            if (newLines.Count > 0)
            {
                var variablesWithNewLinesBuilder = new StringBuilder();

                variablesWithNewLinesBuilder.AppendLine("Variables containing new lines: ");

                foreach (var keyValuePair in newLines)
                {
                    variablesWithNewLinesBuilder.Append("Key ").Append(keyValuePair.Key).AppendLine(": ").AppendLine();
                    variablesWithNewLinesBuilder.Append('\'').Append(keyValuePair.Value).Append('\'').AppendLine();
                }

                _logger.Error("{Variables}", variablesWithNewLinesBuilder.ToString());

                throw new InvalidOperationException(variablesWithNewLinesBuilder.ToString());
            }
        }

        public async Task<ExitCode> RunAsync(string[]? args)
        {
            _args = args ?? Array.Empty<string>();
            MultiSourceKeyValueConfiguration multiSourceKeyValueConfiguration = KeyValueConfigurationManager
                .Add(new UserJsonConfiguration())
                .Add(new EnvironmentVariableKeyValueConfigurationSource())
                .Build();

            string? buildDirArg = args
                .FirstOrDefault(arg => arg.StartsWith("-buildDirectory=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=').LastOrDefault();

            if (!string.IsNullOrWhiteSpace(buildDirArg) && Directory.Exists(buildDirArg))
            {
                Directory.SetCurrentDirectory(buildDirArg);
            }

            StaticKeyValueConfigurationManager.Initialize(multiSourceKeyValueConfiguration);

            const bool debugLoggerEnabled = false;

            DirectoryEntry? sourceDir = null;

            if (DebugHelper.IsDebugging(_environmentVariables))
            {
                sourceDir = await StartWithDebuggerAsync().ConfigureAwait(false);
            }

            _container = await BuildBootstrapper.StartAsync(_logger, _environmentVariables, _specialFolders, sourceDir).ConfigureAwait(false);

            _buildContext = _container.Resolve<BuildContext>();

            _logger.Information("Using logger '{Type}'", _logger.GetType());

            _cancellationToken = CancellationToken.None;

            ExitCode exitCode;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                ExitCode systemToolsResult = await RunSystemToolsAsync(sourceDir).ConfigureAwait(false);

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
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Error running builds tools");
                exitCode = ExitCode.Failure;
            }

            stopwatch.Stop();

            _logger.Information("Arbor.Build total elapsed time in seconds: {TotalSeconds:F}",
                stopwatch.Elapsed.TotalSeconds);

            _ = multiSourceKeyValueConfiguration[WellKnownVariables.BuildApplicationExitDelayInMilliseconds]
                .TryParseInt32(out int exitDelayInMilliseconds, 50);

            if (exitDelayInMilliseconds > 0)
            {
                if (_debugEnabled)
                {
                    _logger.Debug(
                        "Delaying build application exit with {ExitDelayInMilliseconds} milliseconds specified in '{BuildApplicationExitDelayInMilliseconds}'",
                        exitDelayInMilliseconds,
                        WellKnownVariables.BuildApplicationExitDelayInMilliseconds);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds), _cancellationToken)
                    .ConfigureAwait(false);
            }

            return exitCode;
        }

        public void Dispose()
        {
            _container?.Dispose();
            _fileSystem.Dispose();
        }
    }
}