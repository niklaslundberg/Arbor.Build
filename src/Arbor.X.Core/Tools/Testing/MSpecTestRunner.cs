using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
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
            string externalToolsPath =
                buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            var sourceRootOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRootOverride, "");

            string sourceDirectoryPath;

            if (string.IsNullOrWhiteSpace(sourceRootOverride) || !Directory.Exists(sourceRootOverride))
            {
                sourceDirectoryPath = VcsPathHelper.FindVcsRootPath();

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

            var arguments = new List<string>();

            arguments.AddRange(testDlls);

            await
                ProcessRunner.ExecuteAsync(mspecExePath, arguments: arguments, cancellationToken: cancellationToken,
                    standardOutLog: logger.Write, standardErrorAction: logger.WriteError);

            return ExitCode.Success;
        }
    }
}