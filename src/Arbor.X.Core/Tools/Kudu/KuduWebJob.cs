using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Kudu
{
    [Priority(1050)]
    [UsedImplicitly]
    public class KuduWebJob : ITool
    {
        private ILogger _logger;

        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _logger = logger;

            string kuduJobsEnabledKey = WellKnownVariables.KuduJobsEnabled;
            bool kuduWebJobsEnabled = buildVariables.GetBooleanByKey(kuduJobsEnabledKey, false);

            if (!kuduWebJobsEnabled)
            {
                _logger.Write("Kudu web jobs are disabled");
                return Task.FromResult(ExitCode.Success);
            }

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            string rootDir = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            _logger.Write("Kudu web jobs are enabled");

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var sourceRootDirectory = new DirectoryInfo(sourceRoot);
            IReadOnlyCollection<FileInfo> csharpProjectFiles =
                sourceRootDirectory.GetFilesRecursive(new List<string> { ".csproj" }, pathLookupSpecification, rootDir);

            List<KuduWebProjectDetails> kuduWebJobProjects = csharpProjectFiles
                .Select(IsKuduWebJobProject)
                .Where(project => project.IsKuduWebJobProject)
                .ToList();

            if (kuduWebJobProjects.Any())
            {
                logger.Write(string.Join(Environment.NewLine,
                    kuduWebJobProjects.Select(
                        webProject => $"Found Kudu web job project: {webProject}")));
            }
            else
            {
                logger.Write("No Kudu web job projects were found");
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
                                if (line.IndexOf(key,
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
                                            _logger.WriteWarning(
                                                $"A Kudu web job key '{key}' has already been found with value '{existingValue}', new value is different '{line}', using first value");
                                        }
                                    }
                                }
                            });

                            if (foundItems.Count == expectedKeys.Count)
                            {
                                kuduWebJobProject = KuduWebProjectDetails.Create(foundItems[kuduWebJobName],
                                    foundItems[kuduWebJobType], file.FullName);
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
