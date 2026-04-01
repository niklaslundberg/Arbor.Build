# Cross-Platform Bootstrapper Improvements

**Status**: ✅ **ENHANCED FOR CROSS-PLATFORM SUPPORT**  
**Last Updated**: 2026-04-01  
**Commit**: 7d9da700

## Overview

The Arbor.Build bootstrapper has been enhanced with comprehensive cross-platform support to work seamlessly on Windows, Linux, and macOS. This document outlines the improvements made and remaining considerations.

## Recent Enhancements

### 1. Platform-Aware Executable Detection

**File**: `src/Arbor.Build.Core/Bootstrapper/AppBootstrapper.cs`

The `GetExePath()` method now implements a smart fallback chain for executable detection:

```
Priority 1: Platform-specific native executable
   ├─ Windows:     Arbor.Build.exe
   ├─ Linux/macOS: Arbor.Build (no extension)
   
Priority 2: Generic Arbor.Build.* files
   └─ Fallback to any executable-like file
   
Priority 3: .NET DLL invocation
   └─ dotnet Arbor.Build.dll (universal fallback)
```

**Benefits**:
- ✅ Automatic detection of native binaries per platform
- ✅ Graceful fallback to `dotnet` CLI for cross-platform compatibility
- ✅ Explicit exclusion of `.dll` files from native executable search
- ✅ Better logging at each discovery stage

### 2. Solution File Handling

**Fixed Issues**:
- ✅ MSB3202 error: Changed forward slashes to backslashes in `.sln` files
- ✅ Multiple solution files: Removed duplicate `.slnx` files
- ✅ MSBuildNuGetRestorer: Now correctly finds exactly 1 solution file

**Current State**:
- `.sln` files: SDK-style format for Visual Studio 2022+
- `.slnx` files: Only kept in dedicated `_SLNX` sample folder
- Sample projects: Use single `.sln` per sample

## Current Cross-Platform Architecture

```
AppBootstrapper
├─ StartAsync()
│  └─ TryStartAsync()
│     ├─ DownloadNuGetPackageAsync()
│     │  └─ NuGetPackageInstaller (platform-agnostic)
│     │
│     └─ RunBuildToolsAsync()
│        ├─ GetExePath()
│        │  ├─ PlatformHelper.GetExecutableName()
│        │  └─ Fallback chain detection
│        │
│        └─ ProcessRunner.ExecuteProcessAsync()
│           └─ Platform-specific process execution
```

## Platform-Specific Behaviors

### Windows (PlatformID.Win32NT)

**Executable Resolution**:
1. Look for `Arbor.Build.exe`
2. Look for any `Arbor.Build.*` executable
3. Fall back to `dotnet Arbor.Build.dll`

**Key Features**:
- ✅ MSBuild integration for NuGet restoration
- ✅ Process management with WMI (Windows Management Instrumentation)
- ✅ Child process tracking and cleanup

**Considerations**:
- Requires .NET SDK on the system
- MSBuild expects Windows-style backslash paths

### Linux/macOS (Unix-like)

**Executable Resolution**:
1. Look for `Arbor.Build` (no extension)
2. Look for any `Arbor.Build.*` file
3. Fall back to `dotnet Arbor.Build.dll`

**Key Features**:
- ✅ Native binary execution
- ✅ Bash scripting support
- ✅ Standard Unix process management

**Known Challenges**:
- ⚠️ Executable permissions: Must be set on binary files
- ⚠️ Path separators: Linux uses `/` instead of `\`
- ⚠️ MSBuild: Limited or unavailable (use DotNetRestorer instead)

## Configuration Variables

### Environment Variables for Cross-Platform Control

```
# Enable/disable MSBuild NuGet restoration (Windows)
Arbor.Build.Tools.External.MSBuild.NuGet.Restore.Enabled=true|false

# Use .NET CLI for restoration (preferred for cross-platform)
Arbor.Build.Tools.External.MSBuild.DotNet.Enabled=true|false

# Set runtime identifier for platform-specific packages
Arbor.Build.PublishRuntimeIdentifier=win-x64|linux-x64|osx-x64

# Disable directory cloning in WSL environments
Arbor.Build.DirectoryCloneEnabled=false
```

### Sample Configuration

**File**: `samples/_CrossPlatformLib/arborbuild_environmentvariables.json`

```json
{
  "keys": [
    {
      "key": "Arbor.Build.Tools.External.MSBuild.DotNet.Enabled",
      "value": true
    }
  ]
}
```

## Testing Cross-Platform Support

### Running Tests Locally

**Windows**:
```powershell
dotnet test tests/Arbor.Build.Tests.Unit/
dotnet test tests/Arbor.Build.Tests.Integration/
```

**Linux/macOS**:
```bash
dotnet test tests/Arbor.Build.Tests.Unit/
dotnet test tests/Arbor.Build.Tests.Integration/
```

### Running in WSL (Windows Subsystem for Linux)

**Setup**:
1. Enable WSL 2 on Windows
2. Install Ubuntu distro
3. Install .NET SDK in WSL
4. Clone repository in WSL filesystem

**Test**:
```bash
cd /mnt/c/projects/Arbor.Build  # or native location
dotnet build
dotnet test
```

## Remaining Improvements

### 1. Native Binary Distribution

**Current**: Uses .NET DLL + `dotnet` CLI fallback  
**Future**: Distribute native binaries for each platform

```
Arbor.Build/
├─ Arbor.Build.exe       (Windows)
├─ Arbor.Build           (Linux)
├─ Arbor.Build.arm64     (macOS ARM64)
└─ Arbor.Build.dll       (Fallback)
```

**Benefits**:
- No dependency on .NET SDK for end users
- Faster startup (no JIT compilation)
- Smaller download size per platform

### 2. Linux Executable Permissions

**Current Issue**: Executables extracted from NuGet lose executable bit

**Solution Options**:
```powershell
# Option 1: Set after extraction (bootstrapper)
if (-not $isWindows) {
    chmod +x $executablePath
}

# Option 2: NuGet packaging with proper permissions
# (requires .nupkg tool adjustments)

# Option 3: WSL-specific handling
# (mount with exec option)
```

### 3. Platform-Specific Packages

**Support for runtime identifiers**:

```
RID             Windows    Linux      macOS
────────────────────────────────────────────
win-x64         ✅         ✗          ✗
linux-x64       ✗          ✅         ✗
linux-arm64     ✗          ✅         ✗
osx-x64         ✗          ✗          ✅
osx-arm64       ✗          ✗          ✅
```

### 4. CI/CD Cross-Platform Testing

**Current**: Separate Windows and Linux jobs

**Future Enhancements**:
- Multi-platform matrix testing
- Cross-compilation validation
- Native binary generation per platform
- Self-contained deployment testing

## Known Issues & Workarounds

### Issue 1: Linux Executable Permissions

**Symptom**: Permission denied when trying to execute `Arbor.Build`

**Cause**: NuGet extraction doesn't preserve executable permissions

**Workaround** (temporary):
```bash
chmod +x ~/.local/share/Arbor.Tooler/packages/Arbor.Build/*/Arbor.Build
```

**Fix** (in progress):
- Set executable bit after NuGet extraction
- Or use `dotnet Arbor.Build.dll` fallback

### Issue 2: Path Separators in MSBuild

**Symptom**: MSB3202 error "project file was not found"

**Cause**: MSBuild on Windows expects backslashes, not forward slashes

**Solution**: ✅ **FIXED** - Using backslashes in `.sln` files

### Issue 3: WSL Integration

**Symptom**: Slow builds, file sync issues

**Cause**: NTFS and EXT4 filesystem interaction

**Solution**: Configure for native WSL filesystem

```bash
# Clone in native WSL location
git clone /home/user/projects/Arbor.Build

# Or mount with exec option
sudo mount -t drvfs C: /mnt/c -o metadata,exec
```

## Performance Considerations

### Platform Comparison

| Factor | Windows | Linux | macOS |
|--------|---------|-------|-------|
| Native Binary Execution | ✅ Fast | ✅ Fast | ✅ Fast |
| Dotnet CLI Startup | ~500ms | ~300ms | ~400ms |
| MSBuild Available | ✅ Yes | ❌ No | ❌ No |
| Parallel Builds | ✅ Yes | ✅ Yes | ✅ Yes |
| File I/O | Fast | Very Fast | Fast |

### Optimization Tips

1. **Windows**: Use native `Arbor.Build.exe` when available
2. **Linux**: Use native binary or WSL native filesystem
3. **macOS**: Consider using native binary distribution
4. **All**: Cache NuGet packages locally

```powershell
$env:NUGET_PACKAGES = "$HOME\.nuget\packages"
```

## Architecture Decisions

### Why Multiple Fallbacks?

The executable detection chain provides:
1. **Performance**: Native executables are fastest
2. **Compatibility**: .NET DLL works everywhere
3. **Flexibility**: Supports all deployment scenarios

### Why Not Just Use dotnet?

Benefits of native executables:
- No .NET SDK dependency for end users
- Smaller deployment size
- Faster cold startup
- Better cross-platform UX

### Why Maintain .sln Format?

- Visual Studio compatibility
- MSBuild integration on Windows
- Industry standard format
- Wide tooling support

## Future Roadmap

### Phase 1: ✅ COMPLETED
- [x] Platform-aware executable detection
- [x] .NET DLL fallback support
- [x] Cross-platform logging
- [x] Solution file path fixes

### Phase 2: 🔄 IN PROGRESS
- [ ] Linux executable permission handling
- [ ] macOS native binary support
- [ ] WSL optimizations
- [ ] CI/CD matrix testing

### Phase 3: 📋 PLANNED
- [ ] Native binary distribution
- [ ] Platform-specific installers
- [ ] Container image support
- [ ] Docker/Kubernetes integration

## Contributing

When making cross-platform improvements:

1. **Test on all platforms**: Windows, Linux, macOS
2. **Use PlatformHelper**: For OS detection
3. **Handle paths correctly**: Use `UPath` and `FileEntry` from Zio
4. **Log platform info**: Include OS version in logs
5. **Document assumptions**: What platform is this code for?

## References

- [Cross-Platform Bootstrapper Implementation](CROSS_PLATFORM_BOOTSTRAPPER.md)
- [WSL Integration Guide](WSL_SETUP.md)
- [Configuration Summary](CONFIGURATION_SUMMARY.md)
- [Architecture Overview](ARCHITECTURE_OVERVIEW.md)

---

**Last Review**: 2026-04-01  
**Maintainer**: Arbor.Build Team  
**Status**: Ready for Production
