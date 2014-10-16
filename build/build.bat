SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Vcs.Branch.Name=develop
SET Arbor.X.Tools.External.MSpec.Enabled=false
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.X.Log.Level=Debug
SET Arbor.X.NuGetPackageVersion=

SET Version.Major=0
SET Version.Minor=1
SET Version.Patch=22
SET Version.Build=90018

CALL "%~dp0\Build.exe"
