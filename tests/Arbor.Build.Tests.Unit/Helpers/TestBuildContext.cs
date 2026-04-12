using System;
using System.Collections.Generic;
using Arbor.Build.Core.Tools.MSBuild;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit.Helpers;

/// <summary>
/// Factory for creating <see cref="BuildContext"/> instances for testing ITool implementations.
/// Provides a fluent API for configuring test contexts with minimal boilerplate.
/// </summary>
public sealed class TestBuildContext
{
    private readonly IFileSystem _fileSystem;
    private UPath? _sourceRootPath;
    private DirectoryEntry? _sourceRoot;
    private readonly List<string> _configurations = [];
    private BuildConfiguration? _currentBuildConfiguration;

    private TestBuildContext(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>
    /// Creates a new test build context with an in-memory file system.
    /// </summary>
    /// <returns>A new TestBuildContext builder.</returns>
    public static TestBuildContext Create() => new(new MemoryFileSystem());

    /// <summary>
    /// Creates a new test build context with a physical file system.
    /// </summary>
    /// <param name="rootPath">The physical root path for the file system.</param>
    /// <returns>A new TestBuildContext builder.</returns>
    public static TestBuildContext CreateWithPhysicalFileSystem(string rootPath)
    {
        var fileSystem = new PhysicalFileSystem();
        return new TestBuildContext(fileSystem);
    }

    /// <summary>
    /// Creates a new test build context with a custom file system.
    /// </summary>
    /// <param name="fileSystem">The file system to use.</param>
    /// <returns>A new TestBuildContext builder.</returns>
    public static TestBuildContext CreateWithFileSystem(IFileSystem fileSystem) =>
        new(fileSystem ?? throw new ArgumentNullException(nameof(fileSystem)));

    /// <summary>
    /// Sets the source root directory for the build context.
    /// </summary>
    /// <param name="sourceRootPath">The path to the source root directory.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithSourceRoot(UPath sourceRootPath)
    {
        _sourceRootPath = sourceRootPath;
        _sourceRoot = new DirectoryEntry(_fileSystem, sourceRootPath);
        return this;
    }

    /// <summary>
    /// Sets the source root directory for the build context.
    /// </summary>
    /// <param name="sourceRootPath">The string path to the source root directory.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithSourceRoot(string sourceRootPath)
    {
        if (string.IsNullOrWhiteSpace(sourceRootPath))
        {
            throw new ArgumentException("Source root path cannot be null or empty.", nameof(sourceRootPath));
        }

        var path = new UPath(sourceRootPath);
        return WithSourceRoot(path);
    }

    /// <summary>
    /// Adds a build configuration to the context.
    /// </summary>
    /// <param name="configuration">The configuration name (e.g., "Debug", "Release").</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithConfiguration(string configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            throw new ArgumentException("Configuration cannot be null or empty.", nameof(configuration));
        }

        _configurations.Add(configuration);
        return this;
    }

    /// <summary>
    /// Adds multiple build configurations to the context.
    /// </summary>
    /// <param name="configurations">The configuration names.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithConfigurations(params string[] configurations)
    {
        if (configurations == null || configurations.Length == 0)
        {
            throw new ArgumentException("Configurations cannot be null or empty.", nameof(configurations));
        }

        foreach (var config in configurations)
        {
            WithConfiguration(config);
        }

        return this;
    }

    /// <summary>
    /// Sets the current build configuration.
    /// </summary>
    /// <param name="buildConfiguration">The current build configuration.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithCurrentBuildConfiguration(BuildConfiguration buildConfiguration)
    {
        _currentBuildConfiguration = buildConfiguration ?? throw new ArgumentNullException(nameof(buildConfiguration));
        return this;
    }

    /// <summary>
    /// Sets the current build configuration by name.
    /// </summary>
    /// <param name="configurationName">The configuration name (e.g., "Debug", "Release").</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithCurrentBuildConfiguration(string configurationName)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            throw new ArgumentException("Configuration name cannot be null or empty.", nameof(configurationName));
        }

        _currentBuildConfiguration = new BuildConfiguration(configurationName);
        return this;
    }

    /// <summary>
    /// Creates a file at the specified path in the source root.
    /// </summary>
    /// <param name="relativePath">The relative path from the source root.</param>
    /// <param name="content">The file content (optional).</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithFile(string relativePath, string? content = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
        }

        if (!_sourceRootPath.HasValue)
        {
            throw new InvalidOperationException(
                "Source root must be set before adding files. Use WithSourceRoot() first.");
        }

        var filePath = _sourceRootPath.Value / relativePath;
        var fileEntry = new FileEntry(_fileSystem, filePath);

        // Ensure parent directory exists
        var parentDirectory = fileEntry.Parent;
        if (parentDirectory != null && !_fileSystem.DirectoryExists(parentDirectory.Path))
        {
            _fileSystem.CreateDirectory(parentDirectory.Path);
        }

        if (content != null)
        {
            _fileSystem.WriteAllText(filePath, content);
        }
        else
        {
            // Create empty file
            _fileSystem.CreateFile(filePath);
        }

        return this;
    }

    /// <summary>
    /// Creates a directory at the specified path in the source root.
    /// </summary>
    /// <param name="relativePath">The relative path from the source root.</param>
    /// <returns>This builder for method chaining.</returns>
    public TestBuildContext WithDirectory(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
        }

        if (!_sourceRootPath.HasValue)
        {
            throw new InvalidOperationException(
                "Source root must be set before adding directories. Use WithSourceRoot() first.");
        }

        var directoryPath = _sourceRootPath.Value / relativePath;
        if (!_fileSystem.DirectoryExists(directoryPath))
        {
            _fileSystem.CreateDirectory(directoryPath);
        }

        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="BuildContext"/>.
    /// </summary>
    /// <returns>A configured BuildContext ready for testing.</returns>
    public BuildContext Build()
    {
        var context = new BuildContext(_fileSystem);

        // Set source root if configured
        if (_sourceRootPath.HasValue && _sourceRoot is { })
        {
            context.SourceRoot = _sourceRoot;
        }

        // Add configurations
        if (_configurations.Count > 0)
        {
            foreach (var config in _configurations)
            {
                context.Configurations.Add(config);
            }
        }
        else
        {
            // Add default configurations if none specified
            context.Configurations.Add("Debug");
            context.Configurations.Add("Release");
        }

        // Set current build configuration if specified
        if (_currentBuildConfiguration is { })
        {
            context.CurrentBuildConfiguration = _currentBuildConfiguration;
        }

        return context;
    }

    /// <summary>
    /// Gets the underlying file system for direct manipulation in tests if needed.
    /// </summary>
    /// <returns>The IFileSystem instance.</returns>
    public IFileSystem GetFileSystem() => _fileSystem;
}
