using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Extensions;
using Arbor.X.Core.IO;
using DelegateLogger = Arbor.Sorbus.Core.DelegateLogger;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Versioning
{
    [Priority(200)]
    public class AssemblyInfoPatcher : ITool
    {
        string _filePattern;

        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var delegateLogger = new DelegateLogger(error: logger.WriteError, warning: logger.WriteWarning,
                info: logger.Write, verbose: logger.WriteVerbose, debug: logger.WriteDebug) { LogLevel = Sorbus.Core.LogLevel.TryParse(logger.LogLevel.Level) };

            var app = new AssemblyPatcherApp(delegateLogger);

            bool assemblyVersionPatchingEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, defaultValue: true);

            if (!assemblyVersionPatchingEnabled)
            {
                logger.WriteWarning("Assembly version patching is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            _filePattern = buildVariables.GetVariableValueOrDefault(WellKnownVariables.AssemblyFilePatchingFilePattern,
                "AssemblyInfo.cs");

            logger.WriteVerbose(string.Format("Using assembly version file pattern '{0}' to lookup files to patch", _filePattern));

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            IVariable netAssemblyVersionVar =
                buildVariables.SingleOrDefault(@var => @var.Key == WellKnownVariables.NetAssemblyVersion);
            string netAssemblyVersion;

            if (netAssemblyVersionVar == null || string.IsNullOrWhiteSpace(netAssemblyVersionVar.Value))
            {
                logger.WriteWarning(string.Format("The build variable {0} is not defined or empty",
                    WellKnownVariables.NetAssemblyVersion));
                netAssemblyVersion = "0.0.1.0";

                logger.WriteWarning(string.Format("Using fall-back version {0}", netAssemblyVersion));
            }
            else
            {
                netAssemblyVersion = netAssemblyVersionVar.Value;
            }

            var assemblyVersion = new Version(netAssemblyVersion);


            IVariable netAssemblyFileVersionVar =
                buildVariables.SingleOrDefault(@var => @var.Key == WellKnownVariables.NetAssemblyFileVersion);
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

            if (buildVariables.GetBooleanByKey(WellKnownVariables.NetAssemblyMetadataEnabled, defaultValue: false))
            {
                var company = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, defaultValue: null);
                var description = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyDescription, defaultValue: null);
                var configuration = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyConfiguration, defaultValue: null);
                var copyright = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCopyright, defaultValue: null);
                var product = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyProduct, defaultValue: null);
                var trademark = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyTrademark, defaultValue: null);

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
                    .GetFilesRecursive(new[] { ".cs"}, defaultPathLookupSpecification, rootDir: sourceRoot)
                    .Where(file => file.Name.Equals(_filePattern, StringComparison.InvariantCultureIgnoreCase))
                    .Select(file => new AssemblyInfoFile(file.FullName))
                    .ToReadOnlyCollection();

                logger.WriteDebug(string.Format("Using file pattern '{0}' to find assembly info files. Found these files: [{3}] {1}{2}", _filePattern, Environment.NewLine, string.Join(Environment.NewLine, assemblyFiles.Select(item => " * " + item.FullPath)), assemblyFiles.Count));

                app.Patch(new AssemblyVersion(assemblyVersion), new AssemblyFileVersion(assemblyFileVersion), sourceRoot, assemblyFiles, assemblyMetaData: assemblyMetadata);
            }
            catch (Exception ex)
            {
                logger.WriteError(string.Format("Could not patch assembly infos. {0}", ex));
                return Task.FromResult(ExitCode.Failure);
            }
            return Task.FromResult(ExitCode.Success);
        }
    }
}