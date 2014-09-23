SET Arbor.X.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.X.Vcs.Branch.Name=develop
SET Arbor.X.Tools.External.MSpec.Enabled=false

SET Version.Major=0
SET Version.Minor=1
SET Version.Patch=18
SET Version.Build=10

CALL "%~dp0\Build.exe"
