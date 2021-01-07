using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Paket
{
    [Priority(100)]
    [UsedImplicitly]
    public class PaketRestorer : ITool
    {
        private readonly IFileSystem _fileSystem;

        public PaketRestorer(IFileSystem fileSystem) => _fileSystem = fileSystem;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            if (buildVariables.GetOptionalBooleanByKey(WellKnownVariables.PaketEnabled) != true)
            {
                logger.Information("Paket is disabled by key '{Key}'", WellKnownVariables.PaketEnabled);
                return ExitCode.Success;
            }

            var sourceRoot =
                new DirectoryEntry(_fileSystem, buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow());

            logger.Information("Looking for paket.exe in source root {FullName}", sourceRoot.FullName);

            PathLookupSpecification pathLookupSpecification =
                DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new List<string>());

            FileEntry? paketExe = null;

            var packageSpecifications =
                sourceRoot.GetFilesRecursive(new List<string> { ".exe" }, pathLookupSpecification)
                    .Where(file => file.Name.Equals("paket.exe", StringComparison.Ordinal))
                    .ToList();

            if (packageSpecifications.Count == 0)
            {
                var normalSearch = sourceRoot.EnumerateFiles("paket.exe", SearchOption.AllDirectories)
                    .OrderBy(file => file.FullName.Length).FirstOrDefault();

                if (normalSearch != null)
                {
                    paketExe = normalSearch;
                }
                else
                {
                    logger.Information("Could not find paket.exe, skipping paket restore");
                    return ExitCode.Success;
                }
            }

            if (paketExe == null)
            {
                IReadOnlyCollection<FileEntry> filtered =
                    packageSpecifications.Where(
                            packagePath =>
                                !pathLookupSpecification.IsFileExcluded(
                                    packagePath,
                                    sourceRoot,
                                    logger: logger).Item1)
                        .ToReadOnlyCollection();

                if (filtered.Count == 0)
                {
                    logger.Information("Could not find paket.exe, filtered out: {V}, skipping paket restore",
                        string.Join(", ", packageSpecifications));
                    return ExitCode.Success;
                }

                paketExe = filtered.First();
            }

            logger.Information("Found paket.exe at '{FullName}'", paketExe.FullName);

            UPath? copyFromPath =
                buildVariables.GetVariableValueOrDefault("Arbor.Build.Tools.Paket.CopyExeFromPath")?.ParseAsPath();

            if (copyFromPath.HasValue)
            {
                if (_fileSystem.FileExists(copyFromPath.Value))
                {
                    _fileSystem.CopyFile(copyFromPath.Value, paketExe.FullName, true);
                    logger.Information("Copied paket.exe to {FullName}", paketExe.FullName);
                }
                else
                {
                    logger.Information("The specified paket.exe path '{CopyFromPath}' does not exist", copyFromPath.Value);
                }
            }
            else
            {
                logger.Information("Found no paket.exe to copy");
            }

            Directory.SetCurrentDirectory(sourceRoot.FullName);

            ExitCode exitCode = await ProcessHelper.ExecuteAsync(
                paketExe.FullName,
                new List<string> { "restore" },
                logger,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return exitCode;
        }
    }
}
