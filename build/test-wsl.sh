#!/bin/bash

# WSL Test Runner Script
# Runs all tests in WSL Ubuntu environment with proper PATH configuration

set -e

# Set up .NET path
if [ -z "$(which dotnet)" ] && [ -d "$HOME/.dotnet" ]; then
  export PATH="$HOME/.dotnet:$PATH"
fi

# Verify dotnet is available
if ! command -v dotnet &> /dev/null; then
  echo "❌ ERROR: dotnet CLI not found in WSL"
  echo ""
  echo "Solutions:"
  echo "1. Install .NET 10: curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet"
  echo "2. Add to PATH: export PATH=\$HOME/.dotnet:\$PATH"
  exit 1
fi

# Navigate to workspace
cd /mnt/e/N/Arbor.Build

# Show environment info
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║         WSL UBUNTU TEST EXECUTION                            ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo "📍 Location: $(pwd)"
echo "🔧 Dotnet:  $(dotnet --version)"
echo "🖥️  OS:      $(lsb_release -ds 2>/dev/null || echo 'Unknown')"
echo "🐚 Shell:   $SHELL"
echo ""

# Run tests with detailed output
echo "🧪 Running all integration tests..."
echo ""

dotnet test \
  tests/Arbor.Build.Tests.Integration \
  --configuration Debug \
  --verbosity normal \
  --logger "console;verbosity=normal" \
  "$@"

TEST_EXIT_CODE=$?

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
if [ $TEST_EXIT_CODE -eq 0 ]; then
  echo "║  ✅ ALL TESTS PASSED                                        ║"
else
  echo "║  ❌ TESTS FAILED (Exit Code: $TEST_EXIT_CODE)                        ║"
fi
echo "╚══════════════════════════════════════════════════════════════╝"

exit $TEST_EXIT_CODE
