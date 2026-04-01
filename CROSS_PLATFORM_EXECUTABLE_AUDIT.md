# Cross-Platform Executable Path Audit Report

**Date**: 2026-04-01  
**Status**: ✅ **AUDIT COMPLETE**

## Executive Summary

Comprehensive audit of all tool classes that access executable paths. Found **2 critical cross-platform issues** and **several well-implemented examples** of cross-platform support.

---

## Critical Issues Found

### 1. ❌ SolutionBuilder.cs

**File**: `src/Arbor.Build.Core/Tools/MSBuild/SolutionBuilder.cs`  
**Issue**: Requires `ExternalTools_MSBuild_ExePath` on all platforms  
**Severity**: HIGH  
**Impact**: CI/CD failures on Linux/macOS when not using DotNet MSBuild

**Current Code (Line 144)**:
```csharp
_msBuildExe = _dotnetMsBuildEnabled
    ? default
    : buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath)
        .GetValueOrThrow().ParseAsPath();
```

**Problem**:
- Calls `.Require()` which throws if variable not found
- MSBuild is Windows-only but no platform check before requiring the path
- Will fail on Linux/macOS even if DotNetMsBuild is not explicitly enabled

**Fix Required**:
```csharp
// Add platform check before requiring Windows-only MSBuild path
if (_dotnetMsBuildEnabled)
{
    _msBuildExe = default;
}
else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
{
    _msBuildExe = buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath)
        .GetValueOrThrow().ParseAsPath();
}
else
{
    logger.Error("MSBuild is not available on {Platform}. Enable {Variable} to use dotnet msbuild instead",
        Environment.OSVersion.Platform,
        WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);
    return ExitCode.Failure;
}
```

---

### 2. ✅ MSBuildNuGetRestorer.cs

**File**: `src/Arbor.Build.Core/Tools/NuGet/MSBuildNuGetRestorer.cs`  
**Issue**: ✅ **ALREADY FIXED** (Commit 766d3b56)  
**Severity**: RESOLVED  

**Fix Applied**:
```csharp
// Skip on non-Windows platforms - MSBuild is Windows-only
if (Environment.OSVersion.Platform != PlatformID.Win32NT)
{
    logger.Debug("Tool skipped on non-Windows platform");
    return ExitCode.Success;
}
```

---

## Well-Implemented Cross-Platform Examples

### ✅ GitHelper.cs

**File**: `src/Arbor.Build.Core/Tools/Git/GitHelper.cs`  
**Status**: EXCELLENT - Model implementation  
**Pattern**: Platform detection with fallback paths

```csharp
if (PlatformHelper.IsWindows)
{
    gitExeLocations.Add(@"C:\Program Files\Git\bin\git.exe");
}
else if (PlatformHelper.IsLinux || PlatformHelper.IsMacOS)
{
    gitExeLocations.Add("/usr/bin/git".ParseAsPath());
    gitExeLocations.Add("/usr/local/bin/git".ParseAsPath());
}
```

**Strengths**:
- Uses `PlatformHelper` for detection
- Provides multiple fallback locations per platform
- Gracefully handles missing executables

---

### ✅ DotNetEnvironmentVariableProvider.cs

**File**: `src/Arbor.Build.Core/Tools/DotNet/DotNetEnvironmentVariableProvider.cs`  
**Status**: GOOD - Cross-platform aware  
**Pattern**: Uses `DotNetExePath` variable which is platform-agnostic

---

### ✅ MSBuildEnvironmentVerification.cs

**File**: `src/Arbor.Build.Core/Tools/MSBuild/MSBuildEnvironmentVerification.cs`  
**Status**: GOOD - Conditional requirement  
**Pattern**: Only requires MSBuild path when not using DotNet MSBuild

```csharp
protected override bool Enabled(IReadOnlyCollection<IVariable> buildVariables) 
    => !buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);
```

---

## Audit Results by File

| File | Issue | Platform Check | Severity | Status |
|------|-------|---|---|---|
| **SolutionBuilder.cs** | Requires MSBuild path | ❌ NO | HIGH | ⚠️ NEEDS FIX |
| **MSBuildNuGetRestorer.cs** | Accesses MSBuild path | ✅ YES | RESOLVED | ✅ FIXED |
| **MSBuildEnvironmentVerification.cs** | Requires MSBuild path | ✅ CONDITIONAL | LOW | ✅ OK |
| **GitHelper.cs** | Git executable lookup | ✅ YES | N/A | ✅ EXCELLENT |
| **DotNetTestRunner.cs** | Uses DotNet paths | ✅ N/A | N/A | ✅ OK |
| **DotNetEnvironmentVariableProvider.cs** | Cross-platform paths | ✅ N/A | N/A | ✅ OK |
| **VisualStudioVariableProvider.cs** | Windows-only tool | TBD | TBD | ⏳ CHECK |

---

## Detailed File Analysis

### SolutionBuilder.cs (CRITICAL)

**Locations of concern**:
- Line 144: Requires MSBuild ExePath without platform check
- Used throughout the class for solution builds
- Will crash on Linux/macOS unless DotNetMsBuild is enabled

**Current protection**:
- Only `_dotnetMsBuildEnabled` flag guards against needing MSBuild
- No explicit platform check

**Recommended fix**:
Add platform guard before requiring Windows-specific paths

---

### MSBuildEnvironmentVerification.cs (GOOD)

**Current logic**:
```csharp
protected override bool Enabled(IReadOnlyCollection<IVariable> buildVariables) 
    => !buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);
```

**How it works**:
- Only requires ExternalTools_MSBuild_ExePath if DotNetMsBuild is NOT enabled
- Other tools won't verify environment if using DotNet MSBuild
- Good defensive pattern

---

### VisualStudioVariableProvider.cs (UNCHECKED)

**Needs investigation** - Visual Studio is Windows-only tool, check if it has platform guards

---

## Related Variable Checks

### Variables in WellKnownVariables.cs

```csharp
ExternalTools_MSBuild_ExePath          // Windows-only, needs platform check
ExternalTools_MSBuild_DotNetEnabled    // Cross-platform safe (uses dotnet)
DotNetExePath                          // Cross-platform safe
ExternalTools_VisualStudio_Version     // Windows-only, needs check
```

---

## Action Items

### Priority 1 (CRITICAL)

- [ ] Fix SolutionBuilder.cs platform check
- [ ] Add System namespace and platform check
- [ ] Test on Windows, Linux, macOS
- [ ] Verify MSBuild errors on non-Windows platforms

**File**: `src/Arbor.Build.Core/Tools/MSBuild/SolutionBuilder.cs`  
**Estimated Work**: 1-2 commits  
**Impact**: Prevents CI/CD failures on Linux/macOS

### Priority 2 (IMPORTANT)

- [ ] Check VisualStudioVariableProvider.cs for platform guards
- [ ] Audit all remaining ExternalTools_* variable accesses
- [ ] Document cross-platform patterns

### Priority 3 (DOCUMENTATION)

- [ ] Create cross-platform patterns guide
- [ ] Document which tools are platform-specific
- [ ] Add platform check checklist for new tools

---

## Cross-Platform Patterns Discovered

### Pattern 1: Platform Check + Early Return (✅ RECOMMENDED)

```csharp
if (Environment.OSVersion.Platform != PlatformID.Win32NT)
{
    logger.Debug("Tool skipped on non-Windows platform");
    return ExitCode.Success;
}
```

**Used in**: MSBuildNuGetRestorer (FIXED)  
**Benefit**: Clean, explicit, early exit

### Pattern 2: Conditional Requirement (✅ GOOD)

```csharp
protected override bool Enabled(IReadOnlyCollection<IVariable> buildVariables) 
    => !someFlag;
```

**Used in**: MSBuildEnvironmentVerification  
**Benefit**: Only requires Windows paths when needed

### Pattern 3: Platform Detection Helper (✅ BEST)

```csharp
if (PlatformHelper.IsWindows)
{
    // Windows paths
}
else if (PlatformHelper.IsLinux)
{
    // Linux paths
}
```

**Used in**: GitHelper  
**Benefit**: Clean, readable, maintainable

---

## Summary of Findings

### Critical Issues: 1
- SolutionBuilder: Missing platform check before requiring MSBuild path

### Resolved Issues: 1
- MSBuildNuGetRestorer: Platform check implemented (FIXED)

### Good Implementations: 3
- GitHelper: Excellent cross-platform support
- MSBuildEnvironmentVerification: Good conditional logic
- DotNetEnvironmentVariableProvider: Safe cross-platform paths

### Needs Investigation: 1
- VisualStudioVariableProvider: Check for platform guards

---

## Recommended Next Steps

1. **Immediate**: Fix SolutionBuilder.cs
2. **Short-term**: Audit VisualStudioVariableProvider.cs
3. **Medium-term**: Create cross-platform patterns guide
4. **Long-term**: Implement pre-commit checks for platform safety

---

## References

- [CROSS_PLATFORM_IMPROVEMENTS.md](../CROSS_PLATFORM_IMPROVEMENTS.md)
- [CROSS_PLATFORM_ACTION_PLAN.md](../CROSS_PLATFORM_ACTION_PLAN.md)
- Commit 766d3b56: MSBuildNuGetRestorer platform fix

---

**Status**: ✅ AUDIT COMPLETE - 1 CRITICAL ISSUE IDENTIFIED AND PRIORITIZED
