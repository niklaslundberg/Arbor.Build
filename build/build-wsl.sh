#!/bin/bash
# Cross-platform build script for WSL (Windows Subsystem for Linux)
# This script is designed to run builds in WSL Ubuntu environment

# Ensure dotnet is in PATH
if [ -d "$HOME/.dotnet" ]; then
    export PATH="$HOME/.dotnet:$PATH"
fi

# Navigate to the source root
cd "$(cd "$(dirname "$0")" && cd .. && pwd)" || exit 1

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet CLI not found. Please install .NET 10.0 SDK."
    echo "Run: curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest"
    exit 1
fi

echo "=== Arbor.Build WSL Cross-Platform Build ==="
echo "Platform: Linux (WSL)"
echo "Dotnet version: $(dotnet --version)"
echo "Build directory: $(pwd)"
echo

# Set build variables
export Arbor__Build__NuGetPackageArtifactsSuffix=""
export Arbor__Build__Artifacts="./artifacts"
export Arbor__Build__SourceRoot="."
export Arbor__Build__BranchName="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo 'unknown')"
export Arbor__Build__GitHash="$(git rev-parse HEAD 2>/dev/null || echo 'unknown')"
export Arbor__Build__Version="1.0.0"
export Arbor__Build__ExternalTools_MSBuild_DotNetEnabled="true"

# Install tools
echo "Installing global tools..."
dotnet tool install --global Arbor.Tooler.GlobalTool --version "*-*" --allow-downgrade
dotnet tool install --global Arbor.Build.Bootstrapper --version "*-*" --allow-downgrade

# Run the build
echo "Running build..."
dotnet arbor-build

echo
echo "=== Build completed ==="
