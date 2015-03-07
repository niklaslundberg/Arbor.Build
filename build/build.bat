@ECHO OFF
SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.MSpec.Enabled=true
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=Debug
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled=false
SET Arbor.X.Build.VariableOverrideEnabled=true
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.X.Build.NetAssembly.Configuration=

SET Version.Major=1
SET Version.Minor=0
SET Version.Patch=19
SET Version.Build=106

SET Arbor.X.Build.NetAssembly.MetadataEnabled=true
SET Arbor.X.Build.NetAssembly.Description=A convention-based build tool
SET Arbor.X.Build.NetAssembly.Company=Niklas Lundberg
SET Arbor.X.Build.NetAssembly.Copyright=© Niklas Lundberg 2014-2015
SET Arbor.X.Build.NetAssembly.Trademark=Arbor.X TM
SET Arbor.X.Build.NetAssembly.Product=Arbor.X

SET Arbor.X.Build.Tests.IgnoredCategories=dummyexcluded,dummyexcluded2,dummyexclude3

REM SET Arbor.X.Vcs.Branch.Name=refs/heads/develop-0.1.26
REM SET Arbor.X.Vcs.Branch.Name=

CALL "%~dp0\Build.exe"

REM Restore variables to default

SET Arbor.X.Build.Bootstrapper.AllowPrerelease=
REM SET Arbor.X.Vcs.Branch.Name=
SET Arbor.X.Tools.External.MSpec.Enabled=
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled
SET Arbor.X.VariableOverrideEnabled=
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=
SET Arbor.X.Build.NetAssembly.Configuration=

SET Version.Major=
SET Version.Minor=
SET Version.Patch=
SET Version.Build=