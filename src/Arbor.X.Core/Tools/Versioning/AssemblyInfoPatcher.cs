using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing.Core;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using JetBrains.Annotations;
using DelegateLogger = Arbor.Sorbus.Core.DelegateLogger;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    [Priority(200)]
    public class AssemblyInfoPatcher : ITool
    {
        private string _filePattern;

        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var delegateLogger = new DelegateLogger(logger.WriteError, logger.WriteWarning,
                logger.Write, logger.WriteVerbose, logger.WriteDebug)
            {
                LogLevel = Sorbus.Core.LogLevel.TryParse(logger.LogLevel.Level)
            };

            var app = new AssemblyPatcherApp(delegateLogger);

            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.WriteWarning("Assembly version patching is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            _filePattern = buildVariables.GetVariableValueOrDefault(WellKnownVariables.AssemblyFilePatchingFilePattern,
                "AssemblyInfo.cs");

            logger.WriteVerbose($"Using assembly version file pattern '{_filePattern}' to lookup files to patch");

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            IVariable netAssemblyVersionVar =
                buildVariables.SingleOrDefault(var => var.Key == WellKnownVariables.NetAssemblyVersion);
            string netAssemblyVersion;

            if (netAssemblyVersionVar == null || string.IsNullOrWhiteSpace(netAssemblyVersionVar.Value))
            {
                logger.WriteWarning(
                    $"The build variable {WellKnownVariables.NetAssemblyVersion} is not defined or empty");
                netAssemblyVersion = "0.0.1.0";

                logger.WriteWarning($"Using fall-back version {netAssemblyVersion}");
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
                logger.WriteWarning(
                    $"The build variable {WellKnownVariables.NetAssemblyFileVersion} is not defined or empty");
                netAssemblyFileVersion = "0.0.1.1";

                logger.WriteWarning($"Using fall-back version {netAssemblyFileVersion}");
            }
            else
            {
                netAssemblyFileVersion = netAssemblyFileVersionVar.Value;
            }

            var assemblyFileVersion = new Version(netAssemblyFileVersion);

            AssemblyMetaData assemblyMetadata = null;

            if (buildVariables.GetBooleanByKey(WellKnownVariables.NetAssemblyMetadataEnabled, false))
            {
                string company = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, null);
                string description =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyDescription, null);
                string configuration =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyConfiguration, null);
                string copyright =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCopyright, null);
                string product = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyProduct, null);
                string trademark =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyTrademark, null);

                assemblyMetadata = new AssemblyMetaData(description, configuration, company, product, copyright,
                    trademark);
            }

            try
            {
                logger.WriteVerbose(
                    $"Patching assembly info files with assembly version {assemblyVersion}, assembly file version {assemblyFileVersion} for directory source root directory '{sourceRoot}'");

                var sourceDirectory = new DirectoryInfo(sourceRoot);

                PathLookupSpecification defaultPathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

                IReadOnlyCollection<AssemblyInfoFile> assemblyFiles = sourceDirectory
                    .GetFilesRecursive(new[] { ".cs" }, defaultPathLookupSpecification, sourceRoot)
                    .Where(file => file.Name.Equals(_filePattern, StringComparison.InvariantCultureIgnoreCase))
                    .Select(file => new AssemblyInfoFile(file.FullName))
                    .ToReadOnlyCollection();

                logger.WriteDebug(string.Format(
                    "Using file pattern '{0}' to find assembly info files. Found these files: [{3}] {1}{2}",
                    _filePattern, Environment.NewLine,
                    string.Join(Environment.NewLine, assemblyFiles.Select(item => " * " + item.FullPath)),
                    assemblyFiles.Count));

                app.Patch(new AssemblyVersion(assemblyVersion), new AssemblyFileVersion(assemblyFileVersion),
                    sourceRoot, assemblyFiles, assemblyMetadata);
            }
            catch (Exception ex)
            {
                logger.WriteError($"Could not patch assembly infos. {ex}");
                return Task.FromResult(ExitCode.Failure);
            }
            return Task.FromResult(ExitCode.Success);
        }
    }
}
