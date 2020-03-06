# Arbor.Build

Arbor.Build is a convention-based build tool for .NET Core and .NET Framework. It is built to work with plain .NET-projects and does not try to replace any existing tool like MSBuild but rather invoke other tools based on what is found in a source code repository.

In the simplest form, invoke the bootstrapper, **arbor-build**, within a Git repository and that's all.

Arbor.Build is built to be run both locally on a developer's machine and on a build server. The idea is that the same build process should be able to reproduce in any environment. Features where the build server should be used is to automatically checkout the code, define a unique build number and collect artifacts.

## Usage

* Define environment varibles as the input to the build bootstrapper. The bootstrapper will propagate all environment variables to the build application.
* Run arbor-build

When running Arbor.Build, it will show all the build variables it supports.

## Help

Run arbor-build --help to se available variables to set

## Parts

Arbor.Build consists of two components, a bootstrapper and the build application.

The bootstrapper downloads a specific version or the latest version of the build application as a NuGet package from any NuGet source defined for the current user.

It extracts the NuGet package and invokes the build application.

## Supported models

* Semantic versioning
* .NET Assembly versioning (derived from semantic versioning)
* Git flow
* MSBuild debug/configuration builds
* MSBuild target platform, (x86, x64, ARM, ...)
* Self-contained build
* Deterministic* build (* Some parts of the build will introduce uniqueness for every build)

## Features

* Scan system for supported tooling, like MSBuild, Git and NuGet.
* Invoke MSBuild for all found solution files, built using the configurations and platforms within the solution file
* Run dotnet test for all test projects
* Shows how external processes are invoked in log files to enable reproduction of individual tasks
* Patch and unpatch assembly metadata
* Restore NuGet packages
* Create NuGet packages from found NuSpec-files
* Configurable logging levels
* Logs to standard out
* Azure Web App deployment with Kudu
* NuGet publishing
* Symbols publishing

## License

This code uses the MIT license http://opensource.org/licenses/MIT, see [License.txt](License.txt)

## System requirements

* .NET Core 3.0

## Artifacts

Artifacts are the output of the build process. When Arbor.Build produces and known artifacts, it copies it to a subfolder to /Artifact/. When using Arbor.Build from a build server, it's relatively easy to refer to these artifacts by pattern matching.

Examples of artifacts

* Test report files
* Created NuGet packages
* DLL files and PDB files
* ASP.NET Websites
* EXE files

## Supported Version Control Systems

* Git

## Build variables

Build variables are primarily defined as environment variables. All environment variables defined by Arbor.Build are separated into namespaces.

### Environment variables

All environment variables will be available as build variables with the same name.

## Arbor.Build.Bootstrapper

The bootstrapper can be used in two ways:

1. by adding the binary to the source code
2. by running a custom script to download the latest version of the bootstrapper

The bootstrapper is available as a NuGet package with id Arbor.Build.Bootstrapper

## Azure Web sites Kudu support

Arbor.Build is aware if Azure Web sites and can determine if is running in a context where Kudu is available. This enables web sites to be deployed with source code triggers and the same build pipeline as running elsewhere.

#### Using GIT

The code is cloned from the default repository and checked out with the specified branch into a temporary folder and and build actions are performed from that temp directory. This way the code is always clean.

## MSBuild integration

Find latest version of MSBuild installed on the current machine by looking at registry keys and using vswhere.exe.

### Solution builder

Arbor.Build will scan for Visual Studio solution files .sln and build the solution with all configuration and platform combinations defined in the solution file.

## NuGet

* NuGet package creation from NuGet package specification files .nuspec.
* NuGet package restore
* NuGet package publishing

### Recommended ignored files

* [Tt]emp
* _assemblyPatchInfos
* [Aa]rtifacts
* [Bb]uild/*

## Conventions

### Git branch name

* A Git branch named dev or develop is considered a development branch.
* A Git branch named release-* is considired a release branch
* A Git branch named feature-* is considered a feature branch
* Both '-' and '/' are considired identifier separators 

### NuGet package creation

* Code in a development branch will produce NuGet packages with a suffix, thus the package is considired a pre-release package
* Code in a release branch will produce NuGet packages without suffix, thus the pakage is considered a stable production ready package
* Code in a feature branch will produce packages identified by the feature branch name
* By default no packages are created in a master branch