@ECHO OFF

IF "%Arbor.X.Build.Bootstrapper.AllowPrerelease%" == "" (
    SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
)

SET Arbor.X.Tools.External.MSpec.Enabled=true
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=Debug
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled=false
SET Arbor.X.Build.VariableOverrideEnabled=true
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.X.Build.NetAssembly.Configuration=
SET Arbor.X.Tools.External.LibZ.Enabled=true

SET Arbor.X.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.X.NuGet.VersionUpdateEnabled=false
SET Arbor.X.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.X.NuGet.Package.CreateNuGetWebPackages.Enabled=true

SET Arbor.X.Build.NetAssembly.MetadataEnabled=true
SET Arbor.X.Build.NetAssembly.Description=A convention-based build tool
SET Arbor.X.Build.NetAssembly.Company=Niklas Lundberg
SET Arbor.X.Build.NetAssembly.Copyright=© Niklas Lundberg 2014-2018
SET Arbor.X.Build.NetAssembly.Trademark=Arbor.X TM
SET Arbor.X.Build.NetAssembly.Product=Arbor.X
SET Arbor.X.Tools.External.MSBuild.Verbosity=minimal
SET Arbor.X.NuGet.Package.AllowManifestReWriteEnabled=false
SET Arbor.X.Build.WebDeploy.PreCompilation.Enabled=true
SET Arbor.X.Build.Cleanup.KillProcessesAfterBuild.Enabled=true
SET Arbor.X.NuGet.NuGetWebPackage.ExcludedPatterns=roslyn\**\*.*
SET Arbor.X.Build.Tests.AssemblyStartsWith=Arbor.X.Tests
SET Arbor.X.Tools.External.NUnit.Enabled=false
SET Arbor.X.NuGet.Package.ExcludesCommaSeparated=Arbor.X.Bootstrapper.nuspec
SET Arbor.X.Tools.External.MSBuild.CodeAnalysis.Enabled=false

SET Arbor.X.Build.Tests.IgnoredCategories=dummyexcluded,dummyexcluded2,dummyexclude3


IF "%Arbor.X.ShowAvailableVariablesEnabled%" == "" (
    SET Arbor.X.ShowAvailableVariablesEnabled=false
)

IF "%Arbor.X.ShowDefinedVariablesEnabled%" == "" (
    SET Arbor.X.ShowDefinedVariablesEnabled=false
)

REM SET Arbor.X.Vcs.Branch.Name=refs/heads/develop-0.1.26
REM SET Arbor.X.Vcs.Branch.Name=
REM SET Arbor.X.Vcs.Branch.Name=develop

REM SET Arbor.X.Tools.External.MSBuild.DefaultTarget=Build

CALL dotnet ArborBuild\Arbor.Build.dll

REM Restore variables to default

SET Arbor.X.Build.Bootstrapper.AllowPrerelease=
REM SET Arbor.X.Vcs.Branch.Name=
SET Arbor.X.Tools.External.MSpec.Enabled=
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled=
SET Arbor.X.VariableOverrideEnabled=
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=
SET Arbor.X.Build.NetAssembly.Configuration=

EXIT /B %ERRORLEVEL%
