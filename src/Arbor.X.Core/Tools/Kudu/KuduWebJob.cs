using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Processing.Core;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Kudu
{
    [Priority(1050)]
    [UsedImplicitly]
    public class KuduWebJob : ITool
    {
        private ILogger _logger;

        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _logger = logger;

            string kuduJobsEnabledKey = WellKnownVariables.KuduJobsEnabled;
            bool kuduWebJobsEnabled = buildVariables.GetBooleanByKey(kuduJobsEnabledKey, false);

            if (!kuduWebJobsEnabled)
            {
                _logger.Information("Kudu web jobs are disabled");
                return Task.FromResult(ExitCode.Success);
            }

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string rootDir = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            _logger.Information("Kudu web jobs are enabled");

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var sourceRootDirectory = new DirectoryInfo(sourceRoot);
            IReadOnlyCollection<FileInfo> csharpProjectFiles =
                sourceRootDirectory.GetFilesRecursive(new List<string> { ".csproj" }, pathLookupSpecification, rootDir);

            List<KuduWebProjectDetails> kuduWebJobProjects = csharpProjectFiles
                .Select(IsKuduWebJobProject)
                .Where(project => project.IsKuduWebJobProject)
                .ToList();

            if (kuduWebJobProjects.Count > 0)
            {
                logger.Information(string.Join(
                    Environment.NewLine,
                    kuduWebJobProjects.Select(
                        webProject => $"Found Kudu web job project: {webProject}")));
            }
            else
            {
                logger.Information("No Kudu web job projects were found");
            }

            return Task.FromResult(ExitCode.Success);
        }

        private KuduWebProjectDetails IsKuduWebJobProject(FileInfo file)
        {
            KuduWebProjectDetails kuduWebJobProject = null;

            const string kuduWebJobName = "KuduWebJobName";
            const string kuduWebJobType = "KuduWebJobType";

            var expectedKeys = new List<string> { kuduWebJobName, kuduWebJobType };

            var foundItems = new Dictionary<string, string>();

            using (FileStream fs = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fs))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        string line = streamReader.ReadLine();

                        if (line != null)
                        {
                            expectedKeys.ForEach(key =>
                            {
                                if (line.IndexOf(
                                        key,
                                        StringComparison.InvariantCultureIgnoreCase) >= 0)
                                {
                                    if (!foundItems.ContainsKey(key))
                                    {
                                        foundItems.Add(key, line);
                                    }
                                    else
                                    {
                                        string existingValue = foundItems[key];

                                        if (!existingValue.Equals(line, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            _logger.Warning(
                                                "A Kudu web job key '{Key}' has already been found with value '{ExistingValue}', new value is different '{Line}', using first value",
                                                key,
                                                existingValue,
                                                line);
                                        }
                                    }
                                }
                            });

                            if (foundItems.Count == expectedKeys.Count)
                            {
                                kuduWebJobProject = KuduWebProjectDetails.Create(
                                    foundItems[kuduWebJobName],
                                    foundItems[kuduWebJobType],
                                    file.FullName);
                                break;
                            }
                        }
                    }
                }
            }

            return kuduWebJobProject ?? KuduWebProjectDetails.NotAKuduWebJobProject();
        }
    }
}
