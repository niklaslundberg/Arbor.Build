#!/usr/bin/env bash
# build-local.sh — Run Arbor.Build on itself from source (no global tool installation needed)
#
# Usage:
#   ./build/build-local.sh
#
# This script runs Arbor.Build directly from source using "dotnet run", which means you
# can test local changes without first publishing/installing the bootstrapper.
# It is the recommended way to dog-food Arbor.Build before committing on Linux/macOS.
#
# On Windows use build\build-local.bat (or run this script under WSL).

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "=== Arbor.Build local self-build (Linux/macOS) ==="
echo "Repository root: $REPO_ROOT"
echo ""

# Ensure the solution is built first so the source is up-to-date
dotnet restore Arbor.Build.slnx --configfile .github/nuget.config
dotnet build Arbor.Build.slnx --no-restore --configuration Debug

echo ""
echo "=== Running Arbor.Build on itself (from source) ==="

# Run the build tool directly from source using dotnet run.
# All Arbor.Build variables may be passed as environment variables (using __ as separator).
exec env \
    "Arbor.Build.Tools.External.MSBuild.DotNet.Enabled=true" \
    "Arbor.Build.NuGet.PackageUpload.Enabled=false" \
    "Arbor.Build.Vcs.Branch.BranchModel=GitFlowBuildOnMain" \
    "Arbor.Build.Log.Level=Debug" \
    "Arbor.Build.BuildNumber.UnixEpochSecondsEnabled=true" \
    dotnet run --project src/Arbor.Build --no-build -- "$@"
