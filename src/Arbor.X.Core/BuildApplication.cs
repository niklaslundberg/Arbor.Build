using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Artifacts;
using Arbor.X.Core.Tools.Environments;
using Arbor.X.Core.Tools.ILMerge;
using Arbor.X.Core.Tools.Kudu;
using Arbor.X.Core.Tools.MSBuild;
using Arbor.X.Core.Tools.NuGet;
using Arbor.X.Core.Tools.Symbols;
using Arbor.X.Core.Tools.Testing;
using Arbor.X.Core.Tools.Versioning;
using Arbor.X.Core.Tools.VisualStudio;

namespace Arbor.X.Core
{
    public class BuildApplication
    {
        const string Prefix = "[BuildApplication] ";
        static readonly object ConsoleLock = new object();

        public async Task<int> RunAsync()
        {

            try
            {
                int systemToolsResult = await RunSystemToolsAsync();

                if (systemToolsResult != 0)
                {
                    const string toolsMessage = "All system tools did not succeed";
                    WriteError(toolsMessage);

                    return systemToolsResult;
                }

                WriteNormal("All tools succeeded");
                
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());
                return 1;
            }
            return 0;
        }

        async Task<int> RunSystemToolsAsync()
        {
            var logger = new DelegateLogger(WriteNormal, WriteWarning, WriteError);

            var buildVariables = (await GetBuildVariablesAsync(logger)).ToList();
            
            logger.Write(string.Format("Build variables: [{0}] {1}{2}", buildVariables.Count, Environment.NewLine,
                                       buildVariables.Print()));

            IReadOnlyCollection<ToolWithPriority> toolWithPriorities = ToolFinder.GetTools(logger);

            LogTools(toolWithPriorities, logger);

            int result = 0;

            var toolResults = new List<string>();

            foreach (var toolWithPriority in toolWithPriorities)
            {
                if (result != 0)
                {
                    if (!toolWithPriority.RunAlways)
                    {
                        toolResults.Add(toolWithPriority + " not run");
                        continue;
                    }
                }

                logger.Write(string.Format("Running tool {0}", toolWithPriority));

                try
                {
                    var toolResult = await toolWithPriority.Tool.ExecuteAsync(logger, buildVariables);

                    if (toolResult.IsSuccess)
                    {
                        logger.Write(string.Format("The tool {0} succeeded with exit code {1}", toolWithPriority,
                                                   toolResult));

                        toolResults.Add(toolWithPriority + " succeeded ");
                    }
                    else
                    {
                        logger.WriteError(string.Format("The tool {0} failed with exit code {1}", toolWithPriority,
                                                        toolResult));
                        result = toolResult.Result;

                        toolResults.Add(toolWithPriority + " failed with exit code " + toolResult);
                    }
                }
                catch (Exception ex)
                {
                    toolResults.Add(string.Format("{0} threw {1}", toolWithPriority, ex.GetType().Name));
                    WriteError(string.Format("The tool {0} failed with exception {1}", toolWithPriority, ex));
                    result = 1;
                }
            }

            logger.Write("Tool results:" + Environment.NewLine + string.Join(Environment.NewLine, toolResults));

            if (result != 0)
            {
                return result;
            }

            logger.Write("All tools succeeded");

            return 0;
        }

        void LogTools(IReadOnlyCollection<ToolWithPriority> toolWithPriorities, DelegateLogger logger)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Running tools: [{0}]", toolWithPriorities.Count));
            foreach (var toolWithPriority in toolWithPriorities)
            {
                sb.AppendLine(toolWithPriority.ToString());
            }

            logger.Write(sb.ToString());
        }

        async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(ILogger logger)
        {
            var buildVariables = new List<IVariable>();
            
            var result = await RunOnceAsync();

            buildVariables.AddRange(result);
            
            var environmentVariables = Environment.GetEnvironmentVariables();

            var variables = environmentVariables
                .OfType<DictionaryEntry>()
                .Select(entry => new EnvironmentVariable(entry.Key.ToString(),
                                                         entry.Value.ToString()));

            buildVariables.AddRange(variables);

            var providers = new List<IVariableProvider>
                            {
                                new SourcePathVariableProvider(),
                                new ArtifactsVariableProvider(),
                                new MSBuildVariableProvider(), 
                                new NugetVariableProvider(), 
                                new VisualStudioVariableProvider(), 
                                new VSTestVariableProvider(), 
                                new BuildVersionProvider(), 
                                new ILMergeVariableProvider(),
                                new SymbolsVariableProvider(),
                                new BuildServerVariableProvider(),
                                new KuduEnvironmentVariableProvider()
                            }; //TODO use Autofac

            logger.Write(string.Format("Available variable providers: [{0}]{1}{2}", providers.Count, Environment.NewLine, string.Join(Environment.NewLine, providers.Select(item => item.GetType().Name))));

            foreach (var provider in providers)
            {
                var newVariables = await provider.GetEnvironmentVariablesAsync(logger, buildVariables);

                foreach (var @var in newVariables)
                {
                    if (buildVariables.Any(
                            item => item.Key.Equals(@var.Key, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        logger.WriteWarning(string.Format("The build variable {0} already exists", @var.Key));
                        continue;
                    }

                    buildVariables.Add(@var);
                }
            }

            return buildVariables;
        }

        async Task<IEnumerable<IVariable>> RunOnceAsync()
        {
            var variables = new Dictionary<string, string>();

            string branchName = Environment.GetEnvironmentVariable(WellKnownVariables.BranchName);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                var branchNameResult = await GetBranchNameByAskingGitExeAsync();
                
                if (branchNameResult.Item1 != 0)
                {
                    throw new InvalidOperationException("Could not find the branch name");
                }

                branchName = branchNameResult.Item2;

                variables.Add(WellKnownVariables.BranchName, branchName);
            }

            var configuration = GetConfiguration(branchName);
            variables.Add(WellKnownVariables.Configuration, configuration);
            var isReleaseBuild = IsReleaseBuild(branchName);
            variables.Add(WellKnownVariables.ReleaseBuild, isReleaseBuild.ToString());

            var newLines = variables.Where(item => item.Value.Contains(Environment.NewLine)).ToList();

            if (newLines.Any())
            {
                var variablesWithNewLinesBuilder = new StringBuilder();

                variablesWithNewLinesBuilder.AppendLine("Variables containing new lines: ");

                foreach (var keyValuePair in newLines)
                {
                    variablesWithNewLinesBuilder.AppendLine(string.Format("Key {0}: ", keyValuePair.Key));
                    variablesWithNewLinesBuilder.AppendLine(string.Format("'{0}'", keyValuePair.Value));
                }

                WriteError(variablesWithNewLinesBuilder.ToString());

                throw new InvalidOperationException(variablesWithNewLinesBuilder.ToString());
            }

            return variables.Select(item => new EnvironmentVariable(item.Key, item.Value));
        }

        bool IsReleaseBuild(string branchName)
        {
            if (branchName.StartsWith("release", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        string GetConfiguration(string branchName)
        {
            if (branchName.StartsWith("release", StringComparison.InvariantCultureIgnoreCase))
            {
                return "release";
            }

            return "debug";
        }

        static async Task<Tuple<int, string>> GetBranchNameByAskingGitExeAsync()
        {
            WriteNormal(string.Format("Environment variable '{0}' is not defined or has empty value",
                                      WellKnownVariables.BranchName));

            var gitExePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin",
                             "git.exe");

            if (!File.Exists(gitExePath))
            {
                var githubForWindowsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GitHub");

                if (Directory.Exists(githubForWindowsPath))
                {
                    var shellFile = Path.Combine(githubForWindowsPath, "shell.ps1");

                    if (File.Exists(shellFile))
                    {
                        var lines = File.ReadAllLines(shellFile);

                        var pathLine = lines.SingleOrDefault(
                            line => line.IndexOf("$env:github_git = ", StringComparison.InvariantCultureIgnoreCase) >= 0);

                        if (!string.IsNullOrWhiteSpace(pathLine))
                        {
                            var directory = pathLine.Split('=').Last().Replace("\"", "");

                            var githPath = Path.Combine(directory, "bin", "git.exe");

                            if (File.Exists(githPath))
                            {
                                gitExePath = githPath;
                            }
                        }
                    }
                }

                if (!File.Exists(gitExePath))
                {
                    WriteError(string.Format("Could not find Git. '{0}' does not exist", gitExePath));
                    return Tuple.Create(-1, string.Empty);
                }
            }

            var arguments = new List<string> {"rev-parse", "--abbrev-ref", "HEAD"};


            var currentDirectory = VcsPathHelper.FindVcsRootPath();

            if (currentDirectory == null)
            {
                WriteError("Could not find source root");
                return Tuple.Create(-1, string.Empty);
            }

            var branchName = await GetGitBranchNameAsync(currentDirectory, gitExePath, arguments);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                WriteError("Git branch name was null or empty");
                return Tuple.Create(-1, string.Empty);
            }
            return Tuple.Create(0, branchName);
        }

        static async Task<string> GetGitBranchNameAsync(string currentDirectory, string gitExePath, IEnumerable<string> arguments)
        {
            string branchName;
            var gitBranchBuilder = new StringBuilder();

            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(currentDirectory);

                var result =
                    await
                    ProcessRunner.ExecuteAsync(gitExePath, arguments: arguments, standardErrorAction: WriteError,
                                               standardOutLog: message => gitBranchBuilder.AppendLine(message));

                if (!result.IsSuccess)
                {
                    WriteError(string.Format("Could not get Git branch name. Git process exit code: {0}", result));
                    return null;
                }
                else
                {
                    branchName = gitBranchBuilder.ToString().Trim();
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
            }
            return branchName;
        }

        static DirectoryInfo EnsureScriptDirectoryExists()
        {
            var systemScriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");

            var systemScriptsDir = new DirectoryInfo(systemScriptsPath);

            if (!systemScriptsDir.Exists)
            {
                systemScriptsDir.Create();
            }
            return systemScriptsDir;
        }

        static void WriteSuccess(string message, params object[] items)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(Prefix + message, items);
                Console.ResetColor();
                Debug.WriteLine(Prefix + message, items);
            }
        }

        static void WriteNormal(string message)
        {
            lock (ConsoleLock)
            {
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine();
                    Debug.WriteLine("");
                }
                else
                {
                    Console.WriteLine(Prefix + message);
                    Debug.WriteLine(Prefix + message);
                }
            }
        }

        static void WriteWarning(string message)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine();
                    Debug.WriteLine("");
                }
                else
                {
                    Console.WriteLine(Prefix + message);
                    Debug.WriteLine(Prefix + message);
                }
                Console.ResetColor();
            }
        }

        static void WriteWarning(string message, params object[] items)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine();
                    Debug.WriteLine("");
                }
                else
                {
                    Console.WriteLine(Prefix + message, items);
                    Debug.WriteLine(Prefix + message, items);
                }

                Console.ResetColor();
            }
        }

        static void WriteError(string message)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.Error.WriteLine();
                    Debug.WriteLine("");
                }
                else
                {
                    Console.Error.WriteLine(Prefix + message);
                    Debug.WriteLine(Prefix + message);
                }
                Console.ResetColor();
            }
        }

        static void WriteError(string message, params object[] items)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(Prefix + message, items);
                }
                Console.ResetColor();
            }
        }
    }
}