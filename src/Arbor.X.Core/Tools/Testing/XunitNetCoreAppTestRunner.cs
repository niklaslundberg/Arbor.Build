using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Properties;
using JetBrains.Annotations;
using Xunit;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class XunitNetCoreAppTestRunner : ITool
    {
        private string _sourceRoot;

        public async Task<ExitCode> ExecuteAsync([NotNull] ILogger logger, [NotNull] IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (buildVariables == null)
            {
                throw new ArgumentNullException(nameof(buildVariables));
            }

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppEnabled, false);

            if (!enabled)
            {
                logger.WriteDebug("Xunit .NET Core App test runner is not enabled");
                return ExitCode.Success;
            }

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();
            string xunitDllPath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.XUnitNetCoreAppDllPath, null) ?? Path.Combine(buildVariables.Require(WellKnownVariables.ExternalTools).Value, "xunit", "netcoreapp2.0", "xunit.console.dll");

            logger.WriteDebug($"Using XUnit dll path '{xunitDllPath}'");

            Type theoryType = typeof(TheoryAttribute);
            Type factAttribute = typeof(FactAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { theoryType, factAttribute };

            bool runTestsInReleaseConfiguration =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    true);

            string configuration = runTestsInReleaseConfiguration ? "release" : "debug";

            string assemblyFilePrefix = buildVariables.GetVariableValueOrDefault(WellKnownVariables.TestsAssemblyStartsWith, string.Empty);

            logger.Write($"Finding Xunit test DLL files built with {configuration} in directory '{_sourceRoot}'");
            logger.Write($"Looking for types {string.Join(", ", typesToFind.Select(t => t.FullName))} in directory '{_sourceRoot}'");

            List<string> testDlls = new UnitTestFinder(typesToFind, logger: logger, debugLogEnabled: true)
                .GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration, assemblyFilePrefix: assemblyFilePrefix, targetFrameworkPrefix: FrameworkConstants.NetCoreApp)
                .ToList();

            if (!testDlls.Any())
            {
                logger.Write("Found no .NETCoreApp Assemblies with Xunit tests");
                return ExitCode.Success;
            }

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Write(
                    $"Path to 'dotnet.exe' has not been specified, set variable '{WellKnownVariables.DotNetExePath}' or ensure the dotnet.exe is installed in its standard location");
                return ExitCode.Failure;
            }

            logger.WriteDebug($"Using dotnet.exe in path '{dotNetExePath}'");

            string xmlReportName = $"{Guid.NewGuid()}.xml";

            var arguments = new List<string>();

            string reportFile = Path.Combine(reportPath.Value, "xunit", xmlReportName);

            var reportFileInfo = new FileInfo(reportFile);
            reportFileInfo.Directory.EnsureExists();

            arguments.Add(xunitDllPath);
            arguments.AddRange(testDlls);
            arguments.Add("-nunit");
            arguments.Add(reportFileInfo.FullName);

            ExitCode result = await ProcessRunner.ExecuteAsync(
                dotNetExePath,
                arguments: arguments,
                standardOutLog: logger.Write,
                standardErrorAction: logger.WriteError,
                toolAction: logger.Write,
                cancellationToken: cancellationToken);

            return result;
        }
    }
}
