using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Machine.Specifications;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450)]
    public class MSpecTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSpecEnabled, defaultValue: true);

            if (!enabled)
            {
                logger.WriteWarning("Machine.Specifications not enabled");
                return ExitCode.Success;
            }

            string externalToolsPath =
                buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string sourceRoot =
                buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string testReportDirectoryPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSpec_ReportPath).ThrowIfEmptyValue().Value;

            var sourceRootOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRootOverride, "");

            string sourceDirectoryPath;

            if (string.IsNullOrWhiteSpace(sourceRootOverride) || !Directory.Exists(sourceRootOverride))
            {
                if (sourceRoot == null)
                {
                    throw new InvalidOperationException("Source root cannot be null");
                }

                sourceDirectoryPath = sourceRoot;

            }
            else
            {
                sourceDirectoryPath = sourceRootOverride;
            }

            var directory = new DirectoryInfo(sourceDirectoryPath);
            string mspecExePath = Path.Combine(externalToolsPath, "Machine.Specifications", "mspec-clr4.exe");


            IEnumerable<Type> typesToFind = new List<Type>
                                            {
                                                typeof (It),
                                                typeof (BehaviorsAttribute),
                                                typeof (SubjectAttribute),
                                                typeof (Behaves_like<>),
                                            };
            List<string> testDlls = new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory).ToList();

            if (!testDlls.Any())
            {
                logger.WriteWarning("No DLL files with Machine.Specifications specifications was found");
                return ExitCode.Success;
            }

            var arguments = new List<string>();

            arguments.AddRange(testDlls);

            if (testDlls.Any(dll => dll.IndexOf("arbor", StringComparison.InvariantCultureIgnoreCase) >= 0))
            {
                arguments.Add("--exclude");
                arguments.Add("Arbor_X_Recursive");
            }

            arguments.Add("--xml");
            var timestamp = DateTime.UtcNow.ToString("O").Replace(":",".");
            var fileName = "MSpec_" + timestamp + ".xml";
            var xmlReportPath = Path.Combine(testReportDirectoryPath, "Xml", fileName);

            new FileInfo(xmlReportPath).Directory.EnsureExists();

            arguments.Add(xmlReportPath);
            var htmlPath = Path.Combine(testReportDirectoryPath, "Html", "MSpec_" + timestamp);

            new DirectoryInfo(htmlPath).EnsureExists();

            arguments.Add("--html");
            arguments.Add(htmlPath);

            var environmentVariables = new Dictionary<string, string>();
            
            var exitCode = await
                ProcessRunner.ExecuteAsync(mspecExePath, arguments: arguments, cancellationToken: cancellationToken,
                    standardOutLog: logger.Write, standardErrorAction: logger.WriteError, toolAction: logger.Write, verboseAction: logger.WriteVerbose, environmentVariables: environmentVariables);

            return exitCode;
        }
    }
}