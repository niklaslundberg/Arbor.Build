# Test Explorer Configuration Guide

## Overview
This guide enables **all test frameworks** in Visual Studio Test Explorer, including:
- ✅ **xUnit v3** (primary framework)
- ✅ **MSTest** (sample tests & framework validation)
- ✅ **Machine.Specifications** (BDD-style tests)
- ✅ **NUnit** (if used)

---

## Step 1: Verify Test Adapters are Installed

All test adapters are configured in `.runsettings` and installed via NuGet packages:

### xUnit v3
```xml
<PackageReference Include="xunit.v3" Version="3.2.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
<PackageReference Include="xunit.analyzers" Version="1.27.0" />
```

### MSTest
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
```

### Machine.Specifications
```xml
<PackageReference Include="Machine.Specifications" Version="1.1.3" />
<PackageReference Include="Machine.Specifications.Runner.VisualStudio" Version="2.10.2" />
```

---

## Step 2: Clear Test Explorer Cache

Test Explorer caches test discovery results. Clear the cache to force rediscovery:

### Method A: Visual Studio UI
1. Open **Test** → **Test Explorer** (`Ctrl+E, T`)
2. Click the **⚙️ Settings** gear icon
3. Uncheck and re-check all test options
4. Click **Refresh** button (↻) in Test Explorer

### Method B: Delete Cache Files
PowerShell (as Administrator):
```powershell
# Remove Visual Studio test cache
Remove-Item -Path "$env:LOCALAPPDATA\Microsoft\VisualStudio\*" -Filter "*.cache" -Recurse -Force
Remove-Item -Path "$env:LOCALAPPDATA\Microsoft\VisualStudio\*" -Filter "ComponentModelCache" -Recurse -Force
```

### Method C: Close and Restart Visual Studio
Sometimes the simplest solution works best:
1. **Close Visual Studio** completely
2. **Restart Visual Studio**
3. Open Test Explorer (`Ctrl+E, T`)
4. Wait for tests to be discovered automatically

---

## Step 3: Verify .runsettings Configuration

The `.runsettings` file at the solution root includes:

### Multi-Framework Discovery Settings
```xml
<RunConfiguration>
  <DiscoverInternalTests>true</DiscoverInternalTests>
  <!-- Enables discovery of internal test classes -->
</RunConfiguration>

<TestAdapterPaths>
  <Directory path="." includeSubDirectories="true" />
  <!-- Discovers all test adapters from NuGet packages -->
</TestAdapterPaths>
```

### Framework-Specific Settings

**xUnit v3:**
```xml
<xUnit>
  <ParallelizeAssembly>true</ParallelizeAssembly>
  <ParallelizeTestCollections>true</ParallelizeTestCollections>
  <PreEnumerateTheories>false</PreEnumerateTheories>
  <UseAppDomains>false</UseAppDomains>
</xUnit>
```

**MSTest:**
```xml
<MSTest>
  <ForcedLegacyMode>false</ForcedLegacyMode>
  <EnableBaseClassTestMethodsFromOtherAssemblies>true</EnableBaseClassTestMethodsFromOtherAssemblies>
  <AssemblyResolutionTimeout>10000</AssemblyResolutionTimeout>
</MSTest>
```

**Machine.Specifications:**
```xml
<MSpec>
  <Parallelize>true</Parallelize>
</MSpec>
```

---

## Step 4: Configure Visual Studio Test Settings

### Path to .runsettings
1. **Test** → **Configure Run Settings** → **Select Solution Wide runsettings File**
2. Choose `.runsettings` from the solution root
3. A checkmark appears next to the filename

### Test Explorer Display Options
1. **Test** → **Test Explorer** (`Ctrl+E, T`)
2. Click **⚙️ Settings** (gear icon)
3. Configure:
   - **Group by**: `Traits` or `Outcome`
   - **Run tests after build**: `Enabled` (optional)
   - **Show hierarchical view**: `Enabled`

### Filter and Visibility
1. In Test Explorer search box, ensure it's **empty** (not filtering tests)
2. Make sure no outcome filters are applied (no checkboxes for Failed/Passed only)
3. View all tests across all projects

---

## Step 5: Build and Discover Tests

```powershell
# Build the solution
dotnet build

# Build should complete without errors
# Then Test Explorer will automatically discover all tests
```

### Expected Test Count
After configuration, you should see approximately:
- **xUnit tests**: ~60+ tests across Unit and Integration projects
- **MSTest sample**: 1 test (VSTestDummy.DoNothing)
- **Machine.Specifications**: Multiple BDD tests (if configured)
- **Total**: 60+ discoverable tests

---

## Troubleshooting

### Tests Still Not Showing?

#### Option 1: Force Test Discovery
1. **Test** → **Test Explorer**
2. Click **Refresh** button (↻)
3. Wait 30 seconds for discovery to complete

#### Option 2: Check Build Errors
```powershell
# Ensure clean build
dotnet clean
dotnet build
```
- Tests won't show if the project doesn't build

#### Option 3: Verify .runsettings is Selected
1. **Test** → **Configure Run Settings**
2. Confirm `.runsettings` has a checkmark
3. If not, click to select it

#### Option 4: Reset Visual Studio
1. Close Visual Studio
2. Delete cache:
   ```powershell
   Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\18.0_*" -Recurse -Force
   ```
3. Restart Visual Studio

#### Option 5: Check for Syntax Errors in Tests
Ensure test files have proper attributes:

**xUnit (✅ correct):**
```csharp
using Xunit;

public class MyTests
{
    [Fact]
    public void TestMethod() { }

    [Theory]
    [InlineData(1)]
    public void TestWithData(int value) { }
}
```

**MSTest (✅ correct):**
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MyTests
{
    [TestMethod]
    public void TestMethod() { }
}
```

---

## Test Execution

Once all tests are discovered:

### Run All Tests
1. **Test** → **Run All Tests** (or `Ctrl+R, A`)
2. Results appear in Test Explorer

### Run Specific Tests
- **Right-click** on test → **Run** or **Debug**
- Double-click a test to run it

### Group Tests
In Test Explorer, use the grouping dropdown to organize by:
- **Project**: Group by test project
- **Traits**: Group by test categories/traits
- **Outcome**: Group by Pass/Fail/Skip status

---

## Multi-Framework Test Example

### Test File with xUnit
```csharp
using Xunit;

namespace Arbor.Build.Tests.Unit;

public class BuildVersionProviderTests
{
    [Fact]
    public async Task Should()
    {
        // Arrange
        var provider = new BuildVersionProvider();
        
        // Act
        var variables = await provider.GetBuildVariablesAsync();
        
        // Assert
        Assert.NotEmpty(variables);
    }
}
```

### Sample Test File with MSTest
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.Build.Tests.Integration.Tests;

/// <summary>
/// Sample test demonstrating MSTest framework support
/// Real tests use xUnit v3
/// </summary>
[TestClass]
public class VSTestDummy
{
    [TestMethod]
    public void DoNothing() => Assert.IsTrue(true);
}
```

---

## Performance Optimization

For faster test discovery:

1. **Disable Code Coverage** (unless needed)
   - Test → Configure Run Settings → uncheck code coverage options

2. **Use Smaller Scope for Debugging**
   - Run specific test instead of all tests when debugging

3. **Parallel Execution** (configured in .runsettings)
   ```xml
   <MaxCpuCount>0</MaxCpuCount>  <!-- Uses all processors -->
   <ParallelizeAssembly>true</ParallelizeAssembly>
   <ParallelizeTestCollections>true</ParallelizeTestCollections>
   ```

4. **Disable Pre-enumeration** for faster discovery
   ```xml
   <PreEnumerateTheories>false</PreEnumerateTheories>
   ```

---

## Useful Test Explorer Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+E, T` | Open Test Explorer |
| `Ctrl+R, A` | Run All Tests |
| `Ctrl+R, T` | Run Current Test |
| `Ctrl+R, D` | Debug Current Test |
| `Ctrl+Shift+T` | Run tests with coverage |

---

## Summary

✅ **All test frameworks are now discoverable:**
1. xUnit v3 (primary)
2. MSTest (framework validation)
3. Machine.Specifications (BDD)
4. NUnit (if used)

✅ **Configuration complete:**
1. `.runsettings` configured for multi-framework support
2. Test adapters installed via NuGet
3. Visual Studio Test Explorer optimized

✅ **Tests should now:**
- Auto-discover in Test Explorer
- Run in parallel
- Support debugging
- Group and filter properly

If issues persist, use **Option 1-4** in the Troubleshooting section.
