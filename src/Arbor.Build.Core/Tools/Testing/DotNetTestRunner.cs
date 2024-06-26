﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Testing;

[Priority(400)]
[UsedImplicitly]
public class DotNetTestRunner(BuildContext buildContext, IFileSystem fileSystem) : ITestRunnerTool
{
    private const string AnyConfiguration = "[Any]";

    public async Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppV2Enabled, true);

        if (!enabled)
        {
            logger.Information(
                ".NET test runner is not enabled, set variable '{XUnitNetCoreAppV2Enabled}' to true to enable",
                WellKnownVariables.XUnitNetCoreAppV2Enabled);

            return ExitCode.Success;
        }

        logger.Information(
            ".NET test runner is enabled, defined in key {Key}",
            WellKnownVariables.XUnitNetCoreAppV2Enabled);

        IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();

        bool? runTestsInReleaseConfiguration =
            buildVariables.GetOptionalBooleanByKey(
                WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

        bool runTestsInAnyConfiguration =
            buildVariables.GetBooleanByKey(WellKnownVariables.RunTestsInAnyConfigurationEnabled);

        string configuration;

        if (runTestsInAnyConfiguration)
        {
            configuration = "[ANY]";
        }
        else if (runTestsInReleaseConfiguration == true)
        {
            configuration = WellKnownConfigurations.Release;
        }
        else if (buildContext.Configurations.Count == 1)
        {
            configuration = buildContext.Configurations.Single();
        }
        else
        {
            configuration = WellKnownConfigurations.Debug;
        }

        var assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

        string? dotNetExePathValue =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath);

        if (string.IsNullOrWhiteSpace(dotNetExePathValue))
        {
            logger.Error(
                "Path to 'dotnet.exe' has not been specified, set variable '{DotNetExePath}' or ensure the dotnet.exe is installed in its standard location",
                WellKnownVariables.DotNetExePath);

            return ExitCode.Failure;
        }

        var dotNetExePath = dotNetExePathValue.ParseAsPath();

        logger.Debug("Using dotnet.exe in path '{DotNetExePath}'", fileSystem.ConvertPathToInternal(dotNetExePath));

        var candidateProjects =
            buildContext.SourceRoot.GetFilesRecursive(new List<string> {".csproj"},
                    DefaultPaths.DefaultPathLookupSpecification, buildContext.SourceRoot)
                .Where(file =>
                    assemblyFilePrefix.Length == 0 || assemblyFilePrefix.Any(prefix =>
                        file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

        List<FileEntry> testProjects = [];

        async Task IsTestProject(FileEntry fileEntry)
        {
            var msBuildProject = await MsBuildProject.LoadFrom(fileEntry);

            if (msBuildProject.PackageReferences.Any(reference => string.Equals(reference.Package, "Microsoft.NET.Test.SDK" ,StringComparison.OrdinalIgnoreCase)))
            {
                testProjects.Add(fileEntry);
            }
        }

        foreach (var candidateProject in candidateProjects)
        {
            await IsTestProject(candidateProject);
        }

        if (testProjects.Count == 0)
        {
            logger.Information("Could not find any projects with a reference to Microsoft.NET.Test.SDK");
            return ExitCode.Success;
        }

        logger.Information("Found {Count} projects with a reference to Microsoft.NET.Test.SDK", testProjects.Count);

        var testProjectFiles = testProjects.Select(project => project).ToHashSet();

        var exitCode = ExitCode.Success;

        foreach (var testProject in testProjectFiles)
        {
            var directoryEntry = testProject;
            string xmlReportName = $"dotnet.{directoryEntry.Name}.trx";

            var arguments = new List<string> {"test", fileSystem.ConvertPathToInternal(testProject.Path)};

            if (!configuration.Equals(AnyConfiguration, StringComparison.OrdinalIgnoreCase))
            {
                arguments.Add("--configuration");
                arguments.Add(configuration);
            }

            bool xmlEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlEnabled, true);

            var reportFile = UPath.Combine(reportPath.Value!.ParseAsPath(), "dotnet", xmlReportName);

            var reportFileEntry = new FileEntry(fileSystem, reportFile);
            reportFileEntry.Directory.EnsureExists();

            if (xmlEnabled)
            {
                arguments.Add(
                    $"--logger:trx;LogFileName={fileSystem.ConvertPathToInternal(reportFileEntry.FullName)}");
            }

            var result = await ProcessRunner.ExecuteProcessAsync(
                fileSystem.ConvertPathToInternal(dotNetExePath),
                arguments,
                logger.Information,
                logger.Error,
                logger.Information,
                cancellationToken: cancellationToken);

            if (!result.IsSuccess)
            {
                exitCode = result;

                if (xmlEnabled)
                {
                    bool xmlAnalysisEnabled =
                        buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                            true);

                    if (xmlAnalysisEnabled)
                    {
                        logger.Debug(
                            "Feature flag '{XUnitNetCoreAppXmlAnalysisEnabled}' is enabled and the xunit exit code was {Result}, running xml report to find actual result",
                            WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                            result);

                        var projectExitCode = AnalyzeXml(reportFileEntry,
                            message => logger.Debug("{Message}", message));

                        if (!projectExitCode.IsSuccess)
                        {
                            exitCode = projectExitCode;
                        }
                    }
                }
            }

            if (buildVariables.GetBooleanByKey(
                    WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled))
            {
                logger.Verbose(
                    "Transforming TRX test reports to JUnit format");

                DirectoryEntry xmlReportDirectory = reportFileEntry.Directory;

                // ReSharper disable once PossibleNullReferenceException
                IReadOnlyCollection<FileEntry> xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(TestReportXslt.JUnitSuffix, StringComparison.Ordinal))
                    .ToReadOnlyCollection();

                if (xmlReports.Count > 0)
                {
                    foreach (var xmlReport in xmlReports)
                    {
                        logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.ConvertPathToInternal());

                        try
                        {
                            var transformExitCode =
                                TestReportXslt.Transform(xmlReport, Trx2UnitXsl.Xml, logger);

                            if (!transformExitCode.IsSuccess)
                            {
                                return transformExitCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Could not transform '{FullName}'", xmlReport.ConvertPathToInternal());
                            return ExitCode.Failure;
                        }

                        logger.Debug("Successfully transformed '{FullName}' to JUnit XML format",
                            xmlReport.ConvertPathToInternal());
                    }
                }
            }

            if (buildVariables.GetBooleanByKey(
                    WellKnownVariables.XUnitNetCoreAppV2TrxXsltToJunitEnabled))
            {
                logger.Verbose(
                    "Transforming TRX test reports to JUnit format");

                DirectoryEntry xmlReportDirectory = reportFileEntry.Directory;

                // ReSharper disable once PossibleNullReferenceException
                IReadOnlyCollection<FileEntry> xmlReports = xmlReportDirectory
                    .GetFiles("*.trx")
                    .Where(report => !report.Name.EndsWith(TestReportXslt.JUnitSuffix, StringComparison.Ordinal))
                    .ToReadOnlyCollection();

                if (xmlReports.Count > 0)
                {
                    foreach (var xmlReport in xmlReports)
                    {
                        logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.ConvertPathToInternal());

                        try
                        {
                            var transformExitCode =
                                TestReportXslt.Transform(xmlReport, Trx2UnitXsl.TrxTemplate, logger);

                            if (!transformExitCode.IsSuccess)
                            {
                                return transformExitCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Could not transform '{FullName}'", xmlReport.ConvertPathToInternal());
                            return ExitCode.Failure;
                        }

                        logger.Debug("Successfully transformed '{FullName}' to JUnit XML format",
                            xmlReport.ConvertPathToInternal());
                    }
                }
            }
            else
            {
                logger.Verbose(
                    "TRX transformation to JUnit format is disabled, defined in key '{Key}' and '{TrxKey}'",
                    WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled,
                    WellKnownVariables.XUnitNetCoreAppV2TrxXsltToJunitEnabled);
            }
        }

        return exitCode;
    }

    private static ExitCode AnalyzeXml(FileEntry reportFileEntry, Action<string>? logger)
    {
        if (!reportFileEntry.Exists)
        {
            return ExitCode.Failure;
        }

        string fullName = reportFileEntry.ConvertPathToInternal();

        using var fs = reportFileEntry.FileSystem.OpenFile(reportFileEntry.Path, FileMode.Open, FileAccess.Read);
        var document = XDocument.Load(fs);

        XElement[] collections = document.Descendants("assemblies").Descendants("assembly")
            .Descendants("collection").ToArray();

        int testCount = collections.Count(collection =>
            int.TryParse(collection.Attribute("total")?.Value, out int total) && total > 0);

        if (testCount == 0)
        {
            logger?.Invoke($"Found no tests in '{fullName}'");
            return ExitCode.Failure;
        }

        logger?.Invoke($"Found {testCount} tests in '{fullName}'");

        int failedTests = collections.Count(collection =>
            int.TryParse(collection.Attribute("failed")?.Value, out int failed) && failed > 0);

        if (failedTests > 0)
        {
            logger?.Invoke($"Found {failedTests} failing tests in '{fullName}'");
            return ExitCode.Failure;
        }

        return ExitCode.Success;
    }
}