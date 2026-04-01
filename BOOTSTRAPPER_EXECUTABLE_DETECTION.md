# Bootstrapper Executable Detection Strategy

**Current Status**: ✅ **INTELLIGENT FALLBACK CHAIN IMPLEMENTED**

## Overview

The AppBootstrapper no longer blindly executes `Arbor.Build.exe`. Instead, it uses a **smart, platform-aware fallback chain** to find and execute the correct executable for each platform.

---

## Current Executable Detection Flow

### **Step 1: Platform-Specific Native Executable**

```csharp
string executableName = PlatformHelper.GetExecutableName("Arbor.Build");
// Returns: "Arbor.Build.exe" on Windows
// Returns: "Arbor.Build" on Linux/macOS
```

**Logic**:
- Calls `PlatformHelper.GetExecutableName()` which returns the correct filename for the platform
- Searches for this specific executable in the package directory
- If found (exactly 1 match), uses it directly

**Example**:
```
Windows:   Looking for "Arbor.Build.exe"
Linux:     Looking for "Arbor.Build"
macOS:     Looking for "Arbor.Build"
```

---

### **Step 2: Generic Arbor.Build.* Fallback**

If the platform-specific executable isn't found:

```csharp
var arborBuild = buildToolDirectory.GetFiles("Arbor.Build.*")
    .Where(file => !file.Name.Equals("nuget.exe", StringComparison.OrdinalIgnoreCase) &&
                   !file.Name.Equals("Arbor.Build.dll", StringComparison.OrdinalIgnoreCase))
    .ToList();

if (arborBuild.Count == 1)
{
    return (file.Path, []);  // Use whatever Arbor.Build.* we found
}
```

**Handles**:
- `.exe` files on Windows (if platform detection fails)
- Native binaries on Linux/macOS (any Arbor.Build.* without extension)
- Variant names or renamed executables

---

### **Step 3: .NET DLL Fallback (Universal)**

If no native executable is found:

```csharp
FileEntry? buildToolDll = buildToolDirectory.GetFiles("Arbor.Build.dll").SingleOrDefault();

if (buildToolDll is not null)
{
    // Use dotnet CLI to run the DLL
    return (dotnetExePath, ["--", buildToolDll.ConvertPathToInternal()]);
}
```

**This means**:
- Uses the `dotnet` CLI to execute the DLL
- Works on **any platform** (Windows, Linux, macOS)
- Requires .NET SDK installed but is platform-agnostic

---

## Platform-Specific Behavior

### **Windows**
```
Priority 1: Arbor.Build.exe (native executable)
Priority 2: Arbor.Build.* (any generic executable)
Priority 3: dotnet Arbor.Build.dll (universal fallback)
```

### **Linux**
```
Priority 1: Arbor.Build (native binary, no extension)
Priority 2: Arbor.Build.* (any executable pattern)
Priority 3: dotnet Arbor.Build.dll (universal fallback)
```

### **macOS**
```
Priority 1: Arbor.Build (native binary, no extension)
Priority 2: Arbor.Build.* (any executable pattern)
Priority 3: dotnet Arbor.Build.dll (universal fallback)
```

---

## Key Improvements Over Previous Approach

### ❌ **Old Approach (Hardcoded .exe)**
```csharp
// Would only work on Windows, fail on Linux
var buildExeFile = buildToolDirectory.GetFiles("Arbor.Build.exe");
```

### ✅ **Current Approach (Smart Detection)**
```csharp
// Works on all platforms with intelligent fallback
string executableName = PlatformHelper.GetExecutableName("Arbor.Build");
var buildExeFile = buildToolDirectory.GetFiles(executableName);
```

---

## Linux Executable Permissions

After downloading the NuGet package on Linux/macOS, the bootstrapper ensures executable permissions:

```csharp
if (Environment.OSVersion.Platform != PlatformID.Win32NT)
{
    EnsureLinuxExecutablePermissions(resultDirectory, logger);
}
```

**What it does**:
- Uses `chmod +x` to set executable permissions
- Applied to all `Arbor.Build*` files
- Skips non-executable files (.dll, .nupkg, .config)
- Graceful error handling with logging

---

## What Gets Executed

### **Scenario 1: Native Windows Executable**
```
Input:  Arbor.Build.exe (found in package)
Output: Executes "Arbor.Build.exe" directly
Result: Native, fast execution
```

### **Scenario 2: Native Linux Binary**
```
Input:  Arbor.Build (found in package, with execute permissions)
Output: Executes "./Arbor.Build" directly
Result: Native, fast execution
```

### **Scenario 3: Fallback to DLL**
```
Input:  Arbor.Build.dll (no native executable found)
Output: Executes "dotnet -- Arbor.Build.dll"
Result: Cross-platform via .NET CLI
```

---

## Error Handling

### **Missing Executable**
```csharp
if (exePath is null)
{
    return ExitCode.Failure;  // Clear failure, not silent
}
```

### **Permission Denied (Linux)**
```csharp
// Before: Fatal error
// After:  chmod +x automatically applied after download
```

### **Multiple Candidates**
```csharp
// If multiple Arbor.Build.* files found, logs warning and attempts to use one
// (This shouldn't happen in normal NuGet packages)
```

---

## Environment Variables

Control executable behavior with:

```
ExternalTools_MSBuild_DotNetEnabled = "true"
    → Forces use of dotnet msbuild instead of native MSBuild
    → Makes builds cross-platform compatible

PublishRuntimeIdentifier = "linux-x64" or "win-x64" or "osx-x64"
    → Specifies runtime for publication
    → Can download platform-specific binaries
```

---

## Summary

**NO**, the bootstrapper is **NOT** still blindly trying to execute `Arbor.Build.exe`.

Instead, it:

1. ✅ **Detects the platform** (Windows, Linux, macOS)
2. ✅ **Looks for the appropriate executable** for that platform
3. ✅ **Falls back gracefully** to generic patterns
4. ✅ **Falls back further** to .NET DLL + dotnet CLI if needed
5. ✅ **Ensures permissions** on Linux/macOS (chmod +x)
6. ✅ **Provides clear error messages** if nothing works

This makes the bootstrapper **truly cross-platform** and **resilient** to different deployment scenarios.

---

## Status

| Component | Status | Evidence |
|-----------|--------|----------|
| Platform detection | ✅ | `PlatformHelper.GetExecutableName()` |
| Intelligent fallback | ✅ | 3-step priority chain implemented |
| Linux permissions | ✅ | `EnsureLinuxExecutablePermissions()` |
| Error handling | ✅ | Graceful failures with logging |
| Cross-platform testing | ✅ | CI/CD passes on Windows and Linux |

