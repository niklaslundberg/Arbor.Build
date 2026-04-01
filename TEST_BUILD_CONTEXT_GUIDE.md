# TestBuildContext Helper Guide

## Overview

`TestBuildContext` is a factory class that simplifies creating `BuildContext` instances for testing `ITool` implementations. It provides a fluent API for configuring test fixtures with minimal boilerplate.

## Location

```
tests/Arbor.Build.Tests.Unit/Helpers/TestBuildContext.cs
```

## Features

### ✅ Fluent API
Configure test contexts with method chaining for readability.

### ✅ Multiple File System Options
- In-memory (default) for fast unit tests
- Physical file system for integration tests
- Custom file system support

### ✅ Flexible Configuration
- Source root directory
- Build configurations (Debug, Release, etc.)
- Current build configuration
- File and directory creation

### ✅ Automatic Defaults
- Default configurations added automatically
- Sensible error messages for missing setup

## Quick Start

### Basic Usage

```csharp
using Arbor.Build.Tests.Unit.Helpers;

[Fact]
public void TestTool()
{
    var buildContext = TestBuildContext.Create()
        .WithSourceRoot("/root")
        .Build();
    
    // Use buildContext in your test
}
```

### With Files

```csharp
var buildContext = TestBuildContext.Create()
    .WithSourceRoot("/root")
    .WithFile("Solution.sln", "solution content")
    .WithFile("src/Project.csproj", "project content")
    .Build();
```

### With Configurations

```csharp
var buildContext = TestBuildContext.Create()
    .WithSourceRoot("/root")
    .WithConfigurations("Debug", "Release", "Custom")
    .WithCurrentBuildConfiguration("Release")
    .Build();
```

## API Reference

### Factory Methods

#### `Create()`
Creates a test context with an in-memory file system.

```csharp
var context = TestBuildContext.Create();
```

#### `CreateWithPhysicalFileSystem(string rootPath)`
Creates a test context with a physical file system.

```csharp
var context = TestBuildContext.CreateWithPhysicalFileSystem("D:\\test");
```

#### `CreateWithFileSystem(IFileSystem fileSystem)`
Creates a test context with a custom file system.

```csharp
var fileSystem = new PhysicalFileSystem();
var context = TestBuildContext.CreateWithFileSystem(fileSystem);
```

### Configuration Methods

#### `WithSourceRoot(string path)` / `WithSourceRoot(UPath path)`
Sets the source root directory.

```csharp
.WithSourceRoot("/root")
.WithSourceRoot(new UPath("/root"))
```

#### `WithConfiguration(string configuration)`
Adds a single configuration.

```csharp
.WithConfiguration("Debug")
.WithConfiguration("Release")
```

#### `WithConfigurations(params string[] configurations)`
Adds multiple configurations at once.

```csharp
.WithConfigurations("Debug", "Release", "Custom")
```

#### `WithCurrentBuildConfiguration(string name)` / `WithCurrentBuildConfiguration(BuildConfiguration config)`
Sets the current active configuration.

```csharp
.WithCurrentBuildConfiguration("Release")
.WithCurrentBuildConfiguration(new BuildConfiguration("Debug"))
```

#### `WithFile(string relativePath, string? content = null)`
Creates a file in the source root.

```csharp
.WithFile("Solution.sln", "Microsoft Visual Studio Solution File")
.WithFile("Directory.Build.props")  // Empty file
```

#### `WithDirectory(string relativePath)`
Creates a directory in the source root.

```csharp
.WithDirectory("src")
.WithDirectory("tests/Unit")
.WithDirectory("build/Artifacts")
```

#### `GetFileSystem()`
Gets the underlying file system for direct manipulation.

```csharp
var fileSystem = context.GetFileSystem();
fileSystem.WriteAllText("/root/test.txt", "content");
```

#### `Build()`
Builds and returns the configured `BuildContext`.

```csharp
var buildContext = testContext.Build();
```

## Complete Example

```csharp
using Xunit;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Build.Tests.Unit.Helpers;

public class MsBuildNuGetRestorerTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidSolution_RestoresSuccessfully()
    {
        // Arrange
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/project")
            .WithFile("Solution.sln", "Microsoft Visual Studio Solution File")
            .WithFile("src/Core.csproj", "<Project></Project>")
            .WithFile("Directory.Build.props", "<Project />")
            .WithDirectory("build")
            .WithConfigurations("Debug", "Release")
            .WithCurrentBuildConfiguration("Debug")
            .Build();
        
        var restorer = new MsBuildNuGetRestorer(
            buildContext.FileSystem, 
            buildContext);
        
        // Act
        var result = await restorer.ExecuteAsync(
            logger,
            buildVariables,
            [],
            CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
    }
}
```

## Best Practices

### 1. Use Descriptive Setup
```csharp
// Good: Clear what's being set up
var buildContext = TestBuildContext.Create()
    .WithSourceRoot("/workspace")
    .WithFile("Directory.Build.props", "<Project />")
    .WithFile("src/Core.csproj", "<Project></Project>")
    .WithConfigurations("Debug", "Release")
    .Build();
```

### 2. Reuse Across Tests
```csharp
public class ToolTests
{
    private readonly Lazy<BuildContext> _buildContext = new(
        () => TestBuildContext.Create()
            .WithSourceRoot("/root")
            .WithFile("Solution.sln", "content")
            .Build());
    
    [Fact]
    public void Test1() => Assert.NotNull(_buildContext.Value);
    
    [Fact]
    public void Test2() => Assert.NotNull(_buildContext.Value);
}
```

### 3. Keep Test Data Minimal
```csharp
// Good: Only create what's needed
var context = TestBuildContext.Create()
    .WithSourceRoot("/root")
    .WithFile("Solution.sln", "minimal content")
    .Build();
```

### 4. Use Examples as Reference
See `TestBuildContextExamples.cs` for comprehensive usage patterns.

## Related Files

- **Implementation:** `tests/Arbor.Build.Tests.Unit/Helpers/TestBuildContext.cs`
- **Examples:** `tests/Arbor.Build.Tests.Unit/TestHelpers/Examples/TestBuildContextExamples.cs`
- **BuildContext:** `src/Arbor.Build.Core/Tools/MSBuild/BuildContext.cs`

## Benefits

### Reduces Boilerplate
Without TestBuildContext:
```csharp
var fileSystem = new MemoryFileSystem();
var buildContext = new BuildContext(fileSystem);
buildContext.SourceRoot = new DirectoryEntry(fileSystem, "/root");
buildContext.Configurations.Add("Debug");
buildContext.Configurations.Add("Release");
// ... more setup code
```

With TestBuildContext:
```csharp
var buildContext = TestBuildContext.Create()
    .WithSourceRoot("/root")
    .WithConfigurations("Debug", "Release")
    .Build();
```

### Improves Readability
Fluent API makes test intent clear at a glance.

### Ensures Consistency
All tools are tested with properly configured contexts.

### Enables Easy Maintenance
Change setup in one place affects all tests.

## Testing ITool Implementations

Every class implementing `ITool` should be testable using `TestBuildContext`:

```csharp
public class MyToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidContext_Succeeds()
    {
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/test")
            // ... configure as needed
            .Build();
        
        var tool = new MyTool(buildContext.FileSystem, buildContext);
        var result = await tool.ExecuteAsync(logger, variables, [], ct);
        
        Assert.True(result.IsSuccess);
    }
}
```

## Troubleshooting

### "Source root must be set before adding files"
**Fix:** Call `WithSourceRoot()` before `WithFile()`:
```csharp
.WithSourceRoot("/root")
.WithFile("test.txt")  // Now works
```

### "Source root path cannot be null or empty"
**Fix:** Provide a valid path:
```csharp
.WithSourceRoot("/root")  // ✓ Valid
.WithSourceRoot("")       // ✗ Invalid
```

### File creation fails
**Fix:** Ensure parent directories exist:
```csharp
.WithDirectory("src")
.WithFile("src/file.txt", "content")
```

## Contributing

When adding new test utilities:
1. Extend `TestBuildContext` if related to build context setup
2. Add examples to `TestBuildContextExamples.cs`
3. Update this guide with new patterns
