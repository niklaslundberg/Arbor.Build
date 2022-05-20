@ECHO OFF

IF "%Arbor.Build.Bootstrapper.AllowPrerelease%" == "" (
    SET Arbor.Build.Bootstrapper.AllowPrerelease=true
)

IF "%Arbor.Build.Build.Bootstrapper.AllowPrerelease%" == "" (
    SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=true
)

SET Arbor.Build.PublishDotNetExecutableProjects=false
SET Arbor.Build.NuGet.PackageUpload.PackageExcludeStartsWithPatterns=Arbor.Build.Sample
SET Arbor.Build.Vcs.Branch.BranchModel=GitFlowBuildOnMain
SET Arbor.Build.Tools.External.MSpec.Enabled=true
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.Log.Level=Debug
SET Arbor.Build.Vcs.Branch.Name=%GITHUB_REF%
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.Build.VariableOverrideEnabled=true
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.Build.Tools.External.LibZ.Enabled=true
REM SET Arbor.Build.Tools.External.MSBuild.DeterministicBuild.Enabled=true

SET Arbor.Build.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.Build.NuGet.VersionUpdateEnabled=false
SET Arbor.Build.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.Build.NuGet.Package.CreateNuGetWebPackages.Enabled=true
SET Arbor.Build.NuGet.Package.Symbols.Enabled=true

SET Arbor.Build.NetAssembly.MetadataEnabled=true
SET Arbor.Build.NetAssembly.Description=A convention-based build tool
SET Arbor.Build.NetAssembly.Company=Niklas Lundberg
SET Arbor.Build.NetAssembly.Copyright=© Niklas Lundberg 2014-2022
SET Arbor.Build.NetAssembly.Trademark=Arbor.Build TM
SET Arbor.Build.NetAssembly.Product=Arbor.Build
SET Arbor.Build.Tools.External.MSBuild.Verbosity=minimal
SET Arbor.Build.NuGet.Package.AllowManifestReWriteEnabled=true
SET Arbor.Build.WebDeploy.PreCompilation.Enabled=true
SET Arbor.Build.Cleanup.KillProcessesAfterBuild.Enabled=true
SET Arbor.Build.Tools.External.NUnit.Enabled=false
SET Arbor.Build.NuGet.Package.ExcludesCommaSeparated=Arbor.Build.Bootstrapper.nuspec
SET Arbor.Build.Tools.External.MSBuild.CodeAnalysis.Enabled=false
SET Arbor.Build.BuildNumber.UnixEpochSecondsEnabled=true
SET Arbor.Build.NuGet.NuGetWebPackage.ExcludedPatterns=Arbor.Build.Samples
SET Arbor.Build.NuGet.PackageUpload.PackageExcludeStartsWithPatterns=dotnet-

SET Arbor.Build.Tests.IgnoredCategories=dummyexcluded,dummyexcluded2,dummyexclude3

SET Arbor.Build.NuGet.PackageUpload.CheckIfPackagesExistsEnabled=false


IF "%Arbor.Build.ShowAvailableVariablesEnabled%" == "" (
    SET Arbor.Build.ShowAvailableVariablesEnabled=false
)

IF "%Arbor.Build.ShowDefinedVariablesEnabled%" == "" (
    SET Arbor.Build.ShowDefinedVariablesEnabled=false
)

CALL dotnet arbor-build

IF "%ERRORLEVEL%" NEQ "0 (
   EXIT /B %ERRORLEVEL%
)

SET Arbor.Build.Bootstrapper.AllowPrerelease=
SET Arbor.Build.Tools.External.MSpec.Enabled=
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.Log.Level=
SET Arbor.Build.NuGetPackageVersion=
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=
SET Arbor.Build.VariableOverrideEnabled=
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=

EXIT /B %ERRORLEVEL%
