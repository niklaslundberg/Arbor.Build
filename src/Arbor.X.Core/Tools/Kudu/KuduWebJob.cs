using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Kudu
{
    [Priority(1050)]
    public class KuduWebJob : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var sourceRootDirectory = new DirectoryInfo(sourceRoot);
            FileInfo[] csharpProjectFiles = sourceRootDirectory.GetFiles("*.csproj", SearchOption.AllDirectories);

            List<KuduWebProjectDetails> kuduWebJobProjects = csharpProjectFiles
                .Select(IsKuduWebJobProject)
                .Where(project => project.IsKuduWebJobProject)
                .ToList();

            logger.Write(string.Join(Environment.NewLine,
                kuduWebJobProjects.Select(
                    webProject => string.Format("Found kudu web job project: '{0}'", webProject.ProjectFilePath))));

            return Task.FromResult(ExitCode.Success);
        }


        KuduWebProjectDetails IsKuduWebJobProject(FileInfo file)
        {
            KuduWebProjectDetails kuduWebJobProject = null;

            const string kuduWebJobName = "KuduWebJobName";
            const string kuduWebJobType = "KuduWebJobType";

            var expectedKeys = new List<string> {kuduWebJobName, kuduWebJobType};

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
                            expectedKeys
                                .ForEach(key =>
                                {
                                    if (line.IndexOf(key,
                                        StringComparison.InvariantCultureIgnoreCase) >= 0)
                                    {
                                        foundItems.Add(key, line);
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