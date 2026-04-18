#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"

find_latest_package() {
    local package
    package="$(find "$repo_root" -type f -name 'Arbor.Build.Tool.*.nupkg' ! -name '*.symbols.nupkg' ! -name '*.snupkg' -printf '%T@ %p\n' | sort -nr | head -n1 | cut -d' ' -f2-)"
    if [[ -z "$package" ]]; then
        echo "ERROR: No Arbor.Build.Tool.*.nupkg package found under $repo_root"
        echo "Create one first, for example with: dotnet pack $repo_root/src/Arbor.Build/Arbor.Build.csproj -c Release"
        exit 1
    fi
    echo "$package"
}

latest_package="$(find_latest_package)"
package_file_name="$(basename "$latest_package")"
package_version="${package_file_name#Arbor.Build.Tool.}"
package_version="${package_version%.nupkg}"
package_source_dir="$(dirname "$latest_package")"
fake_branch_name="feature/fake-docker-local-tool"

if ! command -v docker >/dev/null 2>&1; then
    echo "ERROR: docker command not found"
    exit 1
fi

mkdir -p "$repo_root/.tmp"
temp_workspace="$(mktemp -d "$repo_root/.tmp/arbor-build-docker-workspace-XXXXXX")"
cleanup() {
    rm -rf "$temp_workspace"
}
trap cleanup EXIT

cp -R "$repo_root/samples/Arbor.Build.Sample.PackageProject" "$temp_workspace/Arbor.Build.Sample.PackageProject"
touch "$temp_workspace/.gitattributes"

cat > "$temp_workspace/Arbor.Build.Sample.PackageProject/arborbuild_environmentvariables.json" <<JSON
{
    "version": "1.0",
    "keys": [
        {
            "key": "Version.Major",
            "value": 1
        },
        {
            "key": "Version.Minor",
            "value": 2
        },
        {
            "key": "Version.Patch",
            "value": 3
        },
        {
            "key": "Version.Build",
            "value": 4
        },
        {
            "key": "Arbor.Build.Tools.External.MSBuild.DeterministicBuild.Enabled",
            "value": false
        },
        {
            "key": "Arbor.Build.Tools.External.MSBuild.DotNet.Enabled",
            "value": true
        },
        {
            "key": "Arbor.Build.NuGet.PackageUpload.Enabled",
            "value": "false"
        },
        {
            "key": "Arbor.Build.NuGet.PackageUpload.ForceUploadEnabled",
            "value": "false"
        },
        {
            "key": "Arbor.Build.BranchName",
            "value": "$fake_branch_name"
        },
        {
            "key": "Arbor.Build.Vcs.Branch.Name",
            "value": "$fake_branch_name"
        }
    ]
}
JSON

echo "Using package: $latest_package"
echo "Using package version: $package_version"
echo "Using fake branch name from environment file: $fake_branch_name"
echo "Using temporary workspace: $temp_workspace"

MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker run --rm -t \
    -v "$temp_workspace:/workspace" \
    -v "$package_source_dir:/local-packages" \
    -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    bash -lc "set -euo pipefail; \
        export HOME=/tmp/arbor-build-home; \
        export DOTNET_CLI_HOME=\$HOME; \
        export PATH=\$HOME/.dotnet/tools:\$PATH; \
        mkdir -p \$HOME; \
        dotnet tool install --global Arbor.Build.Tool --version '$package_version' --add-source /local-packages --ignore-failed-sources; \
        cd /workspace; \
        git init -q; \
        git checkout -b '$fake_branch_name' -q; \
        git config user.email arbor-build-docker@example.local; \
        git config user.name arbor-build-docker; \
        git add .; \
        git commit -m initial -q; \
        cd /workspace/Arbor.Build.Sample.PackageProject; \
        export SourceRoot=/workspace/Arbor.Build.Sample.PackageProject; \
        export Arbor__Build__VariableFileSource__Enabled=true; \
        export Arbor__Build__Tools__External__MSBuild__DotNet__Enabled=true; \
        arbor-build"

echo "SUCCESS: Arbor.Build.Tool installed and sample project build completed in Linux container."