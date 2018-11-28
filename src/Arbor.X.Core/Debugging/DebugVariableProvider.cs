﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Debugging
{
    [UsedImplicitly]
    public class DebugVariableProvider : IVariableProvider
    {
        public int Order => int.MinValue + 1;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            if (!DebugHelper.IsDebugging)
            {
                logger.Verbose("Skipping debug variables, not running in debug mode");
                return Task.FromResult(ImmutableArray<IVariable>.Empty);
            }

            var environmentVariables = new Dictionary<string, string>
            {
                [WellKnownVariables.BranchNameVersionOverrideEnabled] = "false",
                [WellKnownVariables.VariableOverrideEnabled] = "true",

                [WellKnownVariables.BranchName] = "develop",
                [WellKnownVariables.VersionMajor] = "3",
                [WellKnownVariables.VersionMinor] = "0",
                [WellKnownVariables.VersionPatch] = "0",
                [WellKnownVariables.VersionBuild] = "286",
                [WellKnownVariables.GenericXmlTransformsEnabled] = "true",
                [WellKnownVariables.NuGetPackageExcludesCommaSeparated] = "Arbor.X.Bootstrapper.nuspec",
                [WellKnownVariables.NuGetAllowManifestReWrite] = "false",
                [WellKnownVariables.NuGetSymbolPackagesEnabled] = "false",
                [WellKnownVariables.NugetCreateNuGetWebPackagesEnabled] = "true",
                ["Arbor_X_Tests_DummyWebApplication_Arbor_X_NuGet_Package_CreateNuGetWebPackageForProject_Enabled"] =
                    "true",
                [WellKnownVariables.NuGetVersionUpdatedEnabled] = "false",
                [WellKnownVariables.ApplicationMetadataEnabled] = "true",
                [WellKnownVariables.LogLevel] = "information",
                [WellKnownVariables.NugetCreateNuGetWebPackageFilter] = "",
                [WellKnownVariables.WebJobsExcludedFileNameParts] =
                    "Microsoft.Build,Microsoft.CodeAnalysis,Microsoft.CodeDom",
                [WellKnownVariables.WebJobsExcludedDirectorySegments] = "roslyn",
                [WellKnownVariables.AppDataJobsEnabled] = "false",
                [WellKnownVariables.ExternalTools_LibZ_ExePath] = @"C:\Tools\Libz\libz.exe",
                [WellKnownVariables.ExternalTools_LibZ_Enabled] = "false",
                [WellKnownVariables.WebDeployPreCompilationEnabled] = "false",
                [WellKnownVariables.ExcludedNuGetWebPackageFiles] =
                    @"bin\roslyn\*.*,bin\Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll",
                [WellKnownVariables.NUnitExePathOverride] = @"C:\Tools\NUnit\nunit3-console.exe",
                [WellKnownVariables.NUnitTransformToJunitEnabled] = "true",
                [WellKnownVariables.XUnitNetFrameworkEnabled] = "false",
                [WellKnownVariables.NUnitEnabled] = "false",
                [WellKnownVariables.MSpecEnabled] = "true",
                [WellKnownVariables.TestsAssemblyStartsWith] = "Milou",
                [WellKnownVariables.DotNetRestoreEnabled] = "false",
                [WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled] = "true",
                [WellKnownVariables.XUnitNetCoreAppEnabled] = "false",
                [WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled] = "true",
                [WellKnownVariables.AssemblyUseReflectionOnlyMode] = "true",
                [WellKnownVariables.MSBuildNuGetRestoreEnabled] = "true",
                [WellKnownVariables.BranchName] = "develop",
                [WellKnownVariables.DotNetPublishExeProjectsEnabled] = "false",
            };

            Task<ImmutableArray<IVariable>> result = Task.FromResult(environmentVariables.Select(
                pair => (IVariable)
                    new BuildVariable(pair.Key, pair.Value)).ToImmutableArray());

            return result;
        }
    }
}
