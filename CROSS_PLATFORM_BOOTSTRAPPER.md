# Cross-Platform Bootstrapper Packaging

## Overview

The `Arbor.Build.Bootstrapper` project is now configured to create cross-platform NuGet tool packages for Windows, Linux, and macOS.

---

## Configuration

### RuntimeIdentifiers

The project now supports building for multiple platforms:

```xml
<RuntimeIdentifiers Condition="'$(Configuration)' == 'release'">
  win-x64;linux-x64;osx-x64
</RuntimeIdentifiers>
```

**Supported Platforms:**
- `win-x64` - Windows (x64)
- `linux-x64` - Linux (x64)
- `osx-x64` - macOS (x64)

---

## Building Cross-Platform Packages

### Release Build (All Platforms)

```powershell
# Build and package for all platforms
dotnet pack src/Arbor.Build.Bootstrapper -c Release

# This creates:
# bin/Release/Arbor.Build.Bootstrapper.0.0.1.nupkg (win-x64)
# bin/Release/Arbor.Build.Bootstrapper.0.0.1.nupkg (linux-x64)
# bin/Release/Arbor.Build.Bootstrapper.0.0.1.nupkg (osx-x64)
```

### Debug Build (Platform-Agnostic)

```powershell
# Debug builds don't specify RuntimeIdentifiers
# Creates a single package without specific RID
dotnet pack src/Arbor.Build.Bootstrapper -c Debug
```

---

## NuSpec Integration

The `src/Arbor.Build.nuspec` file is configured to include platform-specific binaries:

```xml
<file src="Arbor.Build\bin\$configuration$\net10.0\$runtimeIdentifier$\publish\**\*.*" 
      target="" 
      exclude="*.xml;cs\*.*;..." />
```

The `$runtimeIdentifier$` token will be substituted during the pack process for each target RID.

---

## Package Dependencies

### Windows-Specific

```xml
<PackageReference Include="Microsoft.Win32.Primitives" Version="4.3.0" 
                  Condition="'$(RuntimeIdentifier)' == 'win-x64' OR '$(RuntimeIdentifiers)' != ''" />
```

This ensures Windows-specific primitives are only included when building for Windows.

---

## Installation

### Global Tool Installation

Users can install the tool from NuGet:

```bash
# Latest version
dotnet tool install --global Arbor.Build.Bootstrapper

# Specific version
dotnet tool install --global Arbor.Build.Bootstrapper --version 1.0.0
```

### After Installation

The tool is available as a global command:

```bash
dotnet arbor-build [options]
```

---

## Platform-Specific Build Output

When released on NuGet, each platform gets its own self-contained publish directory:

```
win-x64/
├── dotnet-arbor-build.exe
├── Arbor.Build.Core.dll
└── [dependencies]

linux-x64/
├── dotnet-arbor-build
├── Arbor.Build.Core.dll
└── [dependencies]

osx-x64/
├── dotnet-arbor-build
├── Arbor.Build.Core.dll
└── [dependencies]
```

---

## Publishing to NuGet

### Prerequisites

1. NuGet API key configured
2. Version updated in project properties
3. Release notes prepared

### Publish Command

```powershell
# Build release packages for all platforms
dotnet pack src/Arbor.Build.Bootstrapper -c Release

# Push to NuGet (all platforms in one package)
dotnet nuget push bin/Release/Arbor.Build.Bootstrapper.*.nupkg --api-key $env:NUGET_API_KEY
```

---

## Troubleshooting

### Issue: `$runtimeIdentifier$` not substituted in nuspec

**Solution:** Ensure you're using `dotnet pack` with the csproj, not the nuspec directly. The MSBuild integration handles the substitution.

### Issue: Build fails for specific platform

**Troubleshooting:**
1. Check that .NET SDK supports the target RID
2. Verify platform-specific dependencies are available
3. For Linux builds on Windows, consider using WSL:
   ```bash
   wsl dotnet build -r linux-x64
   ```

### Issue: Package size is large

**Cause:** Each platform gets its own complete runtime dependencies.

**Mitigation:** Use trimming or consider runtime-agnostic distribution:
```xml
<PublishTrimmed>true</PublishTrimmed>
<PublishReadyToRun>true</PublishReadyToRun>
```

---

## GitHub Actions Integration

The CI/CD pipeline (`.github/workflows/ci.yml`) should:

1. Build release packages on Windows runner (all platforms)
2. Test on both Windows and Linux runners
3. Publish to NuGet after validation

---

## References

- [.NET Tool Documentation](https://learn.microsoft.com/dotnet/core/tools/global-tools)
- [NuGet Packaging Best Practices](https://learn.microsoft.com/nuget/create-packages/creating-a-package)
- [RID Catalog](https://learn.microsoft.com/dotnet/core/rid-catalog)
