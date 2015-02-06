@ECHO OFF
SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.MSpec.Enabled=false
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=Debug
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled=false
SET Arbor.X.Build.VariableOverrideEnabled=true
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=true

SET Version.Major=1
SET Version.Minor=0
SET Version.Patch=7
SET Version.Build=1

REM SET Arbor.X.Vcs.Branch.Name=refs/heads/develop-0.1.26
SET Arbor.X.Vcs.Branch.Name=release-1.0

CALL "%~dp0\Build.exe"

REM Restore variables to default

SET Arbor.X.Build.Bootstrapper.AllowPrerelease=
SET Arbor.X.Vcs.Branch.Name=
SET Arbor.X.Tools.External.MSpec.Enabled=
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled
SET Arbor.X.VariableOverrideEnabled=
SET Arbor.X.Artifacts.CleanupBeforeBuildEnabled=

SET Version.Major=
SET Version.Minor=
SET Version.Patch=
SET Version.Build=