SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Tools.External.MSpec.Enabled=false
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=Debug
SET Arbor.X.NuGetPackageVersion=
SET Arbor.X.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.X.Build.VariableOverrideEnabled=true

SET Version.Major=0
SET Version.Minor=1
SET Version.Patch=0
SET Version.Build=15

SET Arbor.X.Vcs.Branch.Name=refs/heads/develop-0.1.26
REM SET Arbor.X.Vcs.Branch.Name=develop

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