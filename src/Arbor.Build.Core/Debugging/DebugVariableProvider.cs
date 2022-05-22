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
        private readonly IEnvironmentVariables _environmentVariables;

        public DebugVariableProvider(IEnvironmentVariables environmentVariables) =>
            _environmentVariables = environmentVariables;

        public int Order => int.MinValue + 1;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            if (!DebugHelper.IsDebugging(_environmentVariables))
            {
                logger.Verbose("Skipping debug variables, not running in debug mode");
                return Task.FromResult(ImmutableArray<IVariable>.Empty);
            }

            var environmentVariables = new Dictionary<string, string>
            {
                [WellKnownVariables.BranchNameVersionOverrideEnabled] = "false",
                [WellKnownVariables.VariableOverrideEnabled] = "true",
                [WellKnownVariables.BranchName] = "develop",
                [WellKnownVariables.GenericXmlTransformsEnabled] = "true",
                [WellKnownVariables.NuGetPackageExcludesCommaSeparated] = "Arbor.Build.Bootstrapper.nuspec",
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
                [WellKnownVariables.WebDeployPreCompilationEnabled] = "false",
                //[WellKnownVariables.TestsAssemblyStartsWith] = "",
                [WellKnownVariables.DotNetRestoreEnabled] = "false",
                [WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled] = "true",
                [WellKnownVariables.XUnitNetCoreAppEnabled] = "false",
                [WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled] = "true",
                [WellKnownVariables.AssemblyUseReflectionOnlyMode] = "true",
                [WellKnownVariables.MSBuildNuGetRestoreEnabled] = "true",
                [WellKnownVariables.DotNetPublishExeProjectsEnabled] = "true",
                [WellKnownVariables.NuGetPackageIdBranchNameEnabled] = "false",
                [WellKnownVariables.XUnitNetCoreAppXmlEnabled] = "true",
                [WellKnownVariables.DebugBuildEnabled] = "false",
                [WellKnownVariables.Configuration] = "release",
                [WellKnownVariables.DeterministicBuildEnabled] = "true",
                [WellKnownVariables.RepositoryUrl] = "http://ignore.local",
                [WellKnownVariables.ExternalTools_VisualStudio_Version_Allow_PreRelease] = "true",
                [WellKnownVariables.ExternalTools_MSBuild_AllowPreReleaseEnabled] = "true",
                [WellKnownVariables.PublishRuntimeIdentifier] = "win-x64",
                [WellKnownVariables.GitBranchModel] = "GitFlowBuildOnMaster",
                [WellKnownVariables.NuGetPackageArtifactsSuffix] = "build",
                [WellKnownVariables.NuGetPackageArtifactsSuffixEnabled] = "true"
            };

            Task<ImmutableArray<IVariable>> result = Task.FromResult(environmentVariables.Select(
                pair => (IVariable)
                    new BuildVariable(pair.Key, pair.Value)).ToImmutableArray());

            return result;
        }
    }
}