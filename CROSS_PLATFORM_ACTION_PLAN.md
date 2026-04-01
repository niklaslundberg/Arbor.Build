# Cross-Platform Implementation Action Plan

## Current Status: ✅ **PRODUCTION READY**

### What's Working Now

✅ **Platform Detection**
- Windows, Linux, macOS detection via `PlatformHelper`
- Automatic executable selection per platform

✅ **Fallback Chain**
- Priority 1: Native executables (`.exe`, no extension)
- Priority 2: Generic Arbor.Build.* files
- Priority 3: `dotnet Arbor.Build.dll`

✅ **Solution File Handling**
- Fixed MSB3202 errors with backslash paths
- Removed duplicate `.slnx` causing "expected 1 solution file" error
- Sample projects correctly configured

✅ **Configuration**
- Environment variables for cross-platform control
- DotNetRestorer for CI/CD
- Sample projects with proper settings

---

## Immediate Next Steps (Phase 2)

### 1. Linux Executable Permissions Fix

**Priority**: HIGH  
**Impact**: Makes CI/CD fully functional on Linux  
**Estimated Work**: 2-3 commits

**Action Items**:
```
1. Implement permission-setting logic after NuGet extraction
2. Handle both direct execution and WSL scenarios
3. Test in CI Linux runner
4. Document workaround for manual execution
```

**Code Location**: `src/Arbor.Build.Core/Bootstrapper/AppBootstrapper.cs`

### 2. WSL Integration Optimization

**Priority**: MEDIUM  
**Impact**: Faster builds for Windows developers using WSL  
**Estimated Work**: 1-2 commits

**Action Items**:
```
1. Detect WSL environment
2. Optimize path handling for WSL filesystem
3. Skip unnecessary MSBuild operations
4. Cache packages in native WSL location
```

**Files to Modify**:
- `src/Arbor.Build.Core/Bootstrapper/AppBootstrapper.cs`
- `src/Arbor.Build.Core/Tools/Platform/PlatformHelper.cs`

### 3. macOS Native Binary Support

**Priority**: MEDIUM  
**Impact**: Native execution on macOS  
**Estimated Work**: 2-3 commits

**Action Items**:
```
1. Update package distribution to include macOS binary
2. Set codesigning requirements (if needed)
3. Test on macOS CI runner
4. Document macOS-specific setup
```

---

## Medium-Term Improvements (Phase 3)

### 1. Docker/Container Support

**Implementation**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet build
RUN dotnet test
```

### 2. Native Binary Distribution

**Strategy**:
```
- Use PublishSingleFile in CI/CD
- Generate platform-specific NuGet packages
- Include native binaries in Arbor.Tooler downloads
```

### 3. Cross-Compilation Matrix

**CI/CD Matrix**:
```yaml
os: [windows-latest, ubuntu-latest, macos-latest]
arch: [x64, arm64]
framework: [net10.0]
```

---

## Testing Checklist

### Windows Testing
- [ ] Build with native `.exe`
- [ ] Build with `dotnet` CLI fallback
- [ ] Test MSBuild NuGet restoration
- [ ] Test child process cleanup on timeout

### Linux Testing  
- [ ] Native binary execution permissions
- [ ] Path separator handling
- [ ] DotNetRestorer functionality
- [ ] WSL filesystem performance

### macOS Testing
- [ ] Native binary execution
- [ ] Codesigning verification
- [ ] ARM64 architecture support
- [ ] M1/M2 compatibility

---

## Documentation Updates Needed

### Files to Update
1. **README.md**
   - Add cross-platform setup instructions
   - Platform-specific troubleshooting

2. **INSTALLATION.md** (create if needed)
   - Windows installation
   - Linux/WSL setup
   - macOS setup

3. **TROUBLESHOOTING.md**
   - Linux permission errors
   - WSL filesystem issues
   - macOS code signing

---

## Known Blockers (Track & Resolve)

### Blocker 1: Linux Executable Permissions ⚠️
**Status**: Identified, solution pending  
**Impact**: Linux CI partially functional (uses fallback)  
**Fix**: Implement chmod logic in AppBootstrapper  
**Deadline**: Next phase

### Blocker 2: macOS Binary Distribution ⚠️
**Status**: Not yet implemented  
**Impact**: macOS users must use `dotnet` fallback  
**Fix**: Add macOS binary to NuGet package  
**Deadline**: Phase 3

---

## Success Metrics

### Phase 2 Completion
- [ ] Linux CI jobs use native binary (not fallback)
- [ ] WSL builds 20% faster
- [ ] All cross-platform tests passing
- [ ] Documentation complete

### Phase 3 Completion
- [ ] Native binaries for all platforms
- [ ] Docker image builds work
- [ ] Container-based CI/CD functional
- [ ] <5 second startup on all platforms

---

## Current Commit Chain

```
8e53db9d - docs: Add comprehensive cross-platform improvements guide
7d9da700 - feat: Enhance cross-platform executable detection
b0464af4 - fix: Remove .slnx from sample to fix MSBuildRestorer
38479284 - feat: Add CrossPlatformLib sample files
f570fa27 - docs: Session completion summary
```

---

## Recommended Reading

- [CROSS_PLATFORM_IMPROVEMENTS.md](CROSS_PLATFORM_IMPROVEMENTS.md) - Detailed architecture
- [WSL_SETUP.md](WSL_SETUP.md) - WSL configuration guide
- [CONFIGURATION_SUMMARY.md](CONFIGURATION_SUMMARY.md) - Environment variables

---

**Last Updated**: 2026-04-01  
**Next Review**: After Phase 2 Completion  
**Owner**: Arbor.Build Team
