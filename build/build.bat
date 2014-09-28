SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Vcs.Branch.Name=develop
SET Arbor.X.Tools.External.MSpec.Enabled=false
SET Arbor.X.NuGet.Package.Artifacts.Suffix=
SET Arbor.X.NuGet.Package.Artifacts.BuildNumber.Enabled=

SET Version.Major=0
SET Version.Minor=1
SET Version.Patch=20
SET Version.Build=1

CALL "%~dp0\Build.exe"
