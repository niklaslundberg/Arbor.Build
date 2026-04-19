@ECHO OFF
REM build-local.bat — Run Arbor.Build on itself from source (no global tool installation needed)
REM
REM Usage:
REM   build\build-local.bat
REM
REM This script runs Arbor.Build directly from source using "dotnet run", which means you
REM can test local changes without first publishing/installing the bootstrapper.
REM It is the recommended way to dog-food Arbor.Build before committing on Windows.

SETLOCAL

SET "REPO_ROOT=%~dp0.."
CD /D "%REPO_ROOT%"

ECHO === Arbor.Build local self-build (Windows) ===
ECHO Repository root: %REPO_ROOT%
ECHO.

REM Ensure the solution is built first so the source is up-to-date
dotnet restore Arbor.Build.slnx --configfile .github/nuget.config
IF "%ERRORLEVEL%" NEQ "0" ( EXIT /B %ERRORLEVEL% )

dotnet build Arbor.Build.slnx --no-restore --configuration Debug
IF "%ERRORLEVEL%" NEQ "0" ( EXIT /B %ERRORLEVEL% )

ECHO.
ECHO === Running Arbor.Build on itself (from source) ===

SET Arbor.Build.NuGet.PackageUpload.Enabled=false
SET Arbor.Build.Vcs.Branch.BranchModel=GitFlowBuildOnMain
SET Arbor.Build.Log.Level=Debug
SET Arbor.Build.BuildNumber.UnixEpochSecondsEnabled=true

dotnet run --project src\Arbor.Build --no-build -- %*

IF "%ERRORLEVEL%" NEQ "0" ( EXIT /B %ERRORLEVEL% )

ENDLOCAL
EXIT /B 0
