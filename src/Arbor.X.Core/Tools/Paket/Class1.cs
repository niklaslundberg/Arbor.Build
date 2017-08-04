using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Paket
{
    [Priority(100)]
    [UsedImplicitly]
    public class PaketRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var sourceRoot =
                new DirectoryInfo(buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value);

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

            List<string> packageSpecifications =
                sourceRoot.GetFilesRecursive(new List<string> { ".exe" }, pathLookupSpecification)
                    .Where(file => file.Name.Equals("paket.exe", StringComparison.Ordinal))
                    .Select(f => f.FullName)
                    .ToList();

            IReadOnlyCollection<FileInfo> filtered =
                packageSpecifications.Where(
                        packagePath => !pathLookupSpecification.IsFileBlackListed(packagePath, sourceRoot.FullName))
                    .Select(file => new FileInfo(file))
                    .ToReadOnlyCollection();

            if (!filtered.Any())
            {
                return ExitCode.Success;
            }

            FileInfo first = filtered.First();

            ExitCode exitCode = await ProcessHelper.ExecuteAsync(
                first.FullName,
                new List<string> { "restore" },
                logger,
                cancellationToken: cancellationToken);

            return exitCode;
        }
    }
}
