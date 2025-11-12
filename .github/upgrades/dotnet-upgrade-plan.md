# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10 upgrade.
3. Upgrade src\Arbor.Build.Core\Arbor.Build.Core.csproj
4. Upgrade samples\Arbor.Build.Tests.SampleXunitNetCoreApp31\Arbor.Build.Sample.Tests.XunitNet6.csproj
5. Upgrade samples\Arbor.Build.Sample.PackageProject\Arbor.Build.Sample.PackageProject.csproj
6. Upgrade tests\Arbor.Build.Tests.Unit\Arbor.Build.Tests.Unit.csproj
7. Upgrade tests\Arbor.Build.Tests.Integration\Arbor.Build.Tests.Integration.csproj
8. Upgrade src\Arbor.Build\Arbor.Build.csproj
9. Upgrade src\Arbor.Build.Bootstrapper\Arbor.Build.Bootstrapper.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version | New Version | Description                                   |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Windows.Compatibility     | 9.0.5           | 10.0.0      | Recommended for .NET 10                       |
| Newtonsoft.Json                     | 13.0.3          | 13.0.4      | Recommended for .NET 10                       |
| System.Collections.Immutable        | 9.0.5           | 10.0.0      | Recommended for .NET 10                       |
| System.Net.Http                     | 4.3.4           |             | Functionality included with .NET 10 framework |
| System.Net.Primitives               | 4.3.1           |             | Functionality included with .NET 10 framework |
| System.Reflection.Metadata          | 9.0.5           | 10.0.0      | Recommended for .NET 10                       |
| System.Text.RegularExpressions      | 4.3.1           |             | Functionality included with .NET 10 framework |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### src\Arbor.Build.Core\Arbor.Build.Core.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Windows.Compatibility should be updated from `9.0.5` to `10.0.0` (*recommended for .NET 10*)
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*recommended for .NET 10*)
  - System.Collections.Immutable should be updated from `9.0.5` to `10.0.0` (*recommended for .NET 10*)
  - System.Reflection.Metadata should be updated from `9.0.5` to `10.0.0` (*recommended for .NET 10*)

#### samples\Arbor.Build.Tests.SampleXunitNetCoreApp31\Arbor.Build.Sample.Tests.XunitNet6.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### samples\Arbor.Build.Sample.PackageProject\Arbor.Build.Sample.PackageProject.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4` (*recommended for .NET 10*)

#### tests\Arbor.Build.Tests.Unit\Arbor.Build.Tests.Unit.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - System.Collections.Immutable should be updated from `9.0.5` to `10.0.0` (*recommended for .NET 10*)

#### tests\Arbor.Build.Tests.Integration\Arbor.Build.Tests.Integration.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - System.Net.Http should be removed as its functionality is now included with .NET 10 framework
  - System.Text.RegularExpressions should be removed as its functionality is now included with .NET 10 framework

#### src\Arbor.Build\Arbor.Build.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### src\Arbor.Build.Bootstrapper\Arbor.Build.Bootstrapper.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - System.Reflection.Metadata should be updated from `9.0.5` to `10.0.0` (*recommended for .NET 10*)
  - System.Net.Primitives should be removed as its functionality is now included with .NET 10 framework
