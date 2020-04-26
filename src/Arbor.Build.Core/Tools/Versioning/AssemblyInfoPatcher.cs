using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Sorbus.Core;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Versioning
{
    [UsedImplicitly]
    [Priority(200)]
    public class AssemblyInfoPatcher : ITool
    {
        private string _filePattern = null!;

        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.Warning("Assembly version patching is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            var app = new AssemblyPatcherApp();

            _filePattern = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.AssemblyFilePatchingFilePattern,
                "AssemblyInfo.cs")!;

            logger.Verbose("Using assembly version file pattern '{FilePattern}' to lookup files to patch",
                _filePattern);

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow();

            IVariable netAssemblyVersionVar =
                buildVariables.SingleOrDefault(var => var.Key == WellKnownVariables.NetAssemblyVersion);
            string netAssemblyVersion;

            if (netAssemblyVersionVar == null || string.IsNullOrWhiteSpace(netAssemblyVersionVar.Value))
            {
                logger.Warning("The build variable {NetAssemblyVersion} is not defined or empty",
                    WellKnownVariables.NetAssemblyVersion);
                netAssemblyVersion = "0.0.1.0";

                logger.Warning("Using fall-back version {NetAssemblyVersion}", netAssemblyVersion);
            }
            else
            {
                netAssemblyVersion = netAssemblyVersionVar.Value;
            }

            var assemblyVersion = new Version(netAssemblyVersion);

            IVariable netAssemblyFileVersionVar =
                buildVariables.SingleOrDefault(var => var.Key == WellKnownVariables.NetAssemblyFileVersion);
            string netAssemblyFileVersion;

            if (string.IsNullOrWhiteSpace(netAssemblyFileVersionVar?.Value))
            {
                logger.Warning("The build variable {NetAssemblyFileVersion} is not defined or empty",
                    WellKnownVariables.NetAssemblyFileVersion);
                netAssemblyFileVersion = "0.0.1.1";

                logger.Warning("Using fall-back version {NetAssemblyFileVersion}", netAssemblyFileVersion);
            }
            else
            {
                netAssemblyFileVersion = netAssemblyFileVersionVar.Value;
            }

            var assemblyFileVersion = new Version(netAssemblyFileVersion);

            AssemblyMetaData? assemblyMetadata = null;

            if (buildVariables.GetBooleanByKey(WellKnownVariables.NetAssemblyMetadataEnabled))
            {
                string? company = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany);
                string? description =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyDescription);
                string? configuration =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.CurrentBuildConfiguration);
                string? copyright =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCopyright);
                string? product = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyProduct);
                string? trademark =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyTrademark);

                assemblyMetadata = new AssemblyMetaData(
                    description,
                    configuration,
                    company,
                    product,
                    copyright,
                    trademark);
            }

            try
            {
                logger.Verbose(
                    "Patching assembly info files with assembly version {AssemblyVersion}, assembly file version {AssemblyFileVersion} for directory source root directory '{SourceRoot}'",
                    assemblyVersion,
                    assemblyFileVersion,
                    sourceRoot);

                var sourceDirectory = new DirectoryInfo(sourceRoot);

                PathLookupSpecification defaultPathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

                IReadOnlyCollection<AssemblyInfoFile> assemblyFiles = sourceDirectory
                    .GetFilesRecursive(new[] { ".cs" }, defaultPathLookupSpecification, sourceRoot)
                    .Where(file => file.Name.Equals(_filePattern, StringComparison.OrdinalIgnoreCase))
                    .Select(file => new AssemblyInfoFile(file.FullName))
                    .ToReadOnlyCollection();

                logger.Debug(
                    "Using file pattern '{_filePattern}' to find assembly info files. Found these files: [{Count}] {NewLine}{V}",
                    _filePattern,
                    assemblyFiles.Count,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, assemblyFiles.Select(item => " * " + item.FullPath)));

                app.Patch(
                    new AssemblyVersion(assemblyVersion),
                    new AssemblyFileVersion(assemblyFileVersion),
                    sourceRoot,
                    assemblyFiles,
                    assemblyMetadata);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not patch assembly infos.");
                return Task.FromResult(ExitCode.Failure);
            }

            return Task.FromResult(ExitCode.Success);
        }
    }
}
