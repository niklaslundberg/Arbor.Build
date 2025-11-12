# .NET 10 Upgrade Report

## Project target framework modifications

| Project name                                                                | Old Target Framework | New Target Framework | Commits                                      |
|:----------------------------------------------------------------------------|:--------------------:|:--------------------:|:---------------------------------------------|
| src\Arbor.Build.Core\Arbor.Build.Core.csproj                               | net9.0               | net10.0              | 6fe138b2, 4eae0575                           |
| samples\Arbor.Build.Tests.SampleXunitNetCoreApp31\Arbor.Build.Sample.Tests.XunitNet6.csproj | net9.0      | net10.0              | ed8cdcdf                                     |
| samples\Arbor.Build.Sample.PackageProject\Arbor.Build.Sample.PackageProject.csproj | net9.0         | net10.0              | a67f9b49, a4ab15bf                           |
| tests\Arbor.Build.Tests.Unit\Arbor.Build.Tests.Unit.csproj                 | net9.0               | net10.0              | eaa94de1, 47f9b294                           |
| tests\Arbor.Build.Tests.Integration\Arbor.Build.Tests.Integration.csproj   | net9.0               | net10.0              | 3e6c6ba7                                     |
| src\Arbor.Build\Arbor.Build.csproj                                          | net9.0               | net10.0              | 15cf2ae3                                     |
| src\Arbor.Build.Bootstrapper\Arbor.Build.Bootstrapper.csproj               | net9.0               | net10.0              | 15f10368, d8e1aca8, 42c63817                 |

## NuGet Packages

| Package Name                        | Old Version | New Version | Commit Id                                 |
|:------------------------------------|:-----------:|:-----------:|:------------------------------------------|
| Microsoft.Windows.Compatibility     | 9.0.5       | 10.0.0      | 4eae0575                                  |
| Newtonsoft.Json                     | 13.0.3      | 13.0.4      | 4eae0575, a4ab15bf, 47f9b294              |
| System.Collections.Immutable        | 9.0.5       | 10.0.0      | 4eae0575, 47f9b294                        |
| System.Net.Http                     | 4.3.4       | (removed)   | cb35fccd                                  |
| System.Net.Primitives               | 4.3.1       | (removed)   | 42c63817                                  |
| System.Reflection.Metadata          | 9.0.5       | 10.0.0      | 4eae0575, 42c63817                        |
| System.Text.RegularExpressions      | 4.3.1       | (removed)   | cb35fccd                                  |

## All commits

| Commit ID              | Description                                                                                           |
|:-----------------------|:------------------------------------------------------------------------------------------------------|
| b7fc82ae               | Commit upgrade plan                                                                                   |
| 4eb9340d               | Store final changes for step 'Ensure that the SDK version specified in global.json files is compatible with the .NET 10 upgrade' |
| 122e766d               | Remove unused System.Collections.Immutable and Metadata                                               |
| 6fe138b2               | Update Arbor.Build.Core.csproj to target net10.0                                                      |
| 4eae0575               | Update Arbor.Build.Core dependencies and target framework                                             |
| feb007c5               | Fix C# 14.0 'field' keyword conflict in WellKnownVariables.cs                                         |
| ed8cdcdf               | Update target framework to net10.0 in Arbor.Build.Sample.Tests.XunitNet6.csproj                      |
| a67f9b49               | Update target framework to net10.0 in Arbor.Build.Sample.PackageProject.csproj                       |
| a4ab15bf               | Bump Newtonsoft.Json to 13.0.4 in Arbor.Build.Sample.PackageProject.csproj                           |
| eaa94de1               | Update target framework to net10.0 in Arbor.Build.Tests.Unit.csproj                                  |
| 5b9dfd0a               | Remove System.Collections.Immutable from test project                                                 |
| 47f9b294               | Update to .NET 10, System.Collections.Immutable 10, deps                                              |
| 3e6c6ba7               | Update target framework to net10.0 in Arbor.Build.Tests.Integration.csproj                           |
| cb35fccd               | Remove unused package references in Arbor.Build.Tests.Integration.csproj                             |
| 15cf2ae3               | Update Arbor.Build.csproj to target .NET 10.0                                                         |
| 15f10368               | Update target framework to net10.0 in Arbor.Build.Bootstrapper.csproj                                |
| d8e1aca8               | Remove System.Reflection.Metadata package reference                                                   |
| 42c63817               | Update Arbor.Build.Bootstrapper.csproj dependencies                                                   |

## Project feature upgrades

### src\Arbor.Build.Core\Arbor.Build.Core.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`
- **NuGet Package Updates**: 
  - Microsoft.Windows.Compatibility: 9.0.5 → 10.0.0
  - Newtonsoft.Json: 13.0.3 → 13.0.4
  - System.Collections.Immutable: 9.0.5 → 10.0.0
  - System.Reflection.Metadata: 9.0.5 → 10.0.0
- **C# 14.0 Compatibility Fix**: Renamed variable from `field` to `@field` in WellKnownVariables.cs to avoid conflict with new C# 14.0 keyword

### samples\Arbor.Build.Tests.SampleXunitNetCoreApp31\Arbor.Build.Sample.Tests.XunitNet6.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`

### samples\Arbor.Build.Sample.PackageProject\Arbor.Build.Sample.PackageProject.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`
- **NuGet Package Updates**: Newtonsoft.Json: 13.0.3 → 13.0.4

### tests\Arbor.Build.Tests.Unit\Arbor.Build.Tests.Unit.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`
- **NuGet Package Updates**: System.Collections.Immutable: 9.0.5 → 10.0.0
- **Package Cleanup**: Removed direct dependency on System.Collections.Immutable as it's transitively included

### tests\Arbor.Build.Tests.Integration\Arbor.Build.Tests.Integration.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`
- **Package Removals**: Removed System.Net.Http and System.Text.RegularExpressions as their functionality is now included with .NET 10 framework

### src\Arbor.Build\Arbor.Build.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`

### src\Arbor.Build.Bootstrapper\Arbor.Build.Bootstrapper.csproj

Here is what changed for the project during upgrade:

- **Target Framework Updated**: Changed from `net9.0` to `net10.0`
- **NuGet Package Updates**: System.Reflection.Metadata: 9.0.5 → 10.0.0
- **Package Removals**: Removed System.Net.Primitives as its functionality is now included with .NET 10 framework

## Next steps

- **Build and Test**: Ensure all projects build successfully and run your test suite to verify functionality
- **Review Breaking Changes**: Check for any .NET 10 breaking changes that may affect your code
- **Update CI/CD**: Update your continuous integration pipelines to use .NET 10 SDK
- **Performance Testing**: Validate that the upgrade hasn't introduced any performance regressions
