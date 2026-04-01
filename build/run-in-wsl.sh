#!/bin/bash

# Set up .NET path if needed
if [ -z "$(which dotnet)" ] && [ -d "$HOME/.dotnet" ]; then
  export PATH="$HOME/.dotnet:$PATH"
fi

# Navigate to the workspace (using wsl mnt path)
cd /mnt/e/N/Arbor.Build

# Run Arbor.Build with arguments
echo "===== Running Arbor.Build in WSL Ubuntu ====="
echo "Current directory: $(pwd)"
echo "Dotnet version: $(dotnet --version)"
echo ""

# Run the build
dotnet /mnt/e/N/Arbor.Build/Artifacts/Arbor.Build.Tool/Arbor.Build.dll "$@"
