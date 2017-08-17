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

            logger.Write($"Looking for paket.exe in source root {sourceRoot.FullName}");

            PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new List<string>());

            FileInfo paketExe = null;

            List<string> packageSpecifications =
                sourceRoot.GetFilesRecursive(new List<string> { ".exe" }, pathLookupSpecification)
                    .Where(file => file.Name.Equals("paket.exe", StringComparison.Ordinal))
                    .Select(f => f.FullName)
                    .ToList();

            if (!packageSpecifications.Any())
            {
                FileInfo normalSearch = sourceRoot.GetFiles("paket.exe", SearchOption.AllDirectories)
                    .OrderBy(file => file.FullName.Length).FirstOrDefault();

                if (normalSearch != null)
                {
                    paketExe = normalSearch;
                }
                else
                {
                    logger.Write("Could not find paket.exe, skipping paket restore");
                    return ExitCode.Success;
                }
            }

            if (paketExe == null)
            {
                IReadOnlyCollection<FileInfo> filtered =
                    packageSpecifications.Where(
                            packagePath =>
                                !pathLookupSpecification.IsFileBlackListed(
                                    packagePath,
                                    sourceRoot.FullName,
                                    logger: logger))
                        .Select(file => new FileInfo(file))
                        .ToReadOnlyCollection();

                if (!filtered.Any())
                {
                    logger.Write(
                        $"Could not find paket.exe, filtered out: {string.Join(", ", packageSpecifications)}, skipping paket restore");
                    return ExitCode.Success;
                }

                paketExe = filtered.First();
            }

            logger.Write($"Found paket.exe at '{paketExe.FullName}'");

            string copyFromPath = buildVariables.GetVariableValueOrDefault("Arbor.X.Build.Tools.Paket.CopyExeFromPath", string.Empty);

            if (!string.IsNullOrWhiteSpace(copyFromPath))
            {
                if (File.Exists(copyFromPath))
                {
                    File.Copy(copyFromPath, paketExe.FullName, overwrite: true);
                    logger.Write($"Copied paket.exe to {paketExe.FullName}");
                }
                else
                {
                    logger.Write($"The specified paket.exe path '{copyFromPath}' does not exist");
                }
            }
            else
            {
                logger.Write($"Found no paket.exe to copy");
            }

            ExitCode exitCode = await ProcessHelper.ExecuteAsync(
                paketExe.FullName,
                new List<string> { "restore" },
                logger,
                cancellationToken: cancellationToken);

            return exitCode;
        }
    }
}
