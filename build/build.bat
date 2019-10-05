@ECHO OFF

IF "%Arbor.Build.Build.Bootstrapper.AllowPrerelease%" == "" (
    SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=true
)

SET Arbor.Build.Tools.External.MSpec.Enabled=true
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.Log.Level=Debug
SET Arbor.Build.Vcs.Branch.Name=%GITHUB_REF%
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.Build.Build.VariableOverrideEnabled=true
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.Build.Build.NetAssembly.Configuration=
SET Arbor.Build.Tools.External.LibZ.Enabled=true

SET Arbor.Build.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.Build.NuGet.VersionUpdateEnabled=false
SET Arbor.Build.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.Build.NuGet.Package.CreateNuGetWebPackages.Enabled=true

SET Arbor.Build.Build.NetAssembly.MetadataEnabled=true
SET Arbor.Build.Build.NetAssembly.Description=A convention-based build tool
SET Arbor.Build.Build.NetAssembly.Company=Niklas Lundberg
SET Arbor.Build.Build.NetAssembly.Copyright=© Niklas Lundberg 2014-2019
SET Arbor.Build.Build.NetAssembly.Trademark=Arbor.Build TM
SET Arbor.Build.Build.NetAssembly.Product=Arbor.Build
SET Arbor.Build.Tools.External.MSBuild.Verbosity=minimal
SET Arbor.Build.NuGet.Package.AllowManifestReWriteEnabled=false
SET Arbor.Build.Build.WebDeploy.PreCompilation.Enabled=true
SET Arbor.Build.Build.Cleanup.KillProcessesAfterBuild.Enabled=true
REM SET Arbor.Build.NuGet.NuGetWebPackage.ExcludedPatterns=roslyn\**\*.*
REM SET Arbor.Build.Build.Tests.AssemblyStartsWith=Arbor.Build.Tests
SET Arbor.Build.Tools.External.NUnit.Enabled=false
SET Arbor.Build.NuGet.Package.ExcludesCommaSeparated=Arbor.Build.Bootstrapper.nuspec
SET Arbor.Build.Tools.External.MSBuild.CodeAnalysis.Enabled=false

SET Arbor.Build.Build.Tests.IgnoredCategories=dummyexcluded,dummyexcluded2,dummyexclude3


IF "%Arbor.Build.ShowAvailableVariablesEnabled%" == "" (
    SET Arbor.Build.ShowAvailableVariablesEnabled=false
)

IF "%Arbor.Build.ShowDefinedVariablesEnabled%" == "" (
    SET Arbor.Build.ShowDefinedVariablesEnabled=false
)

CALL dotnet arbor-build

SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=
SET Arbor.Build.Tools.External.MSpec.Enabled=
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.Log.Level=
SET Arbor.Build.NuGetPackageVersion=
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=
SET Arbor.Build.VariableOverrideEnabled=
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=
SET Arbor.Build.Build.NetAssembly.Configuration=

EXIT /B %ERRORLEVEL%
