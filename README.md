# Arbor.X - Convention based build pipeline

# License

MIT TODO

# System requirements

* .NET Framework 4.5.1

# Supported Version Control Systems

* Git
* Mercurial
* Subversion (version TODO)

# Build variables

Build variables are primarily defined as environment variables. All environment variables defined by Arbor.X are separated into namespaces.

# Arbor.X.Bootstrapper

The bootstrapper can be used in two ways:

1. by adding the binary to the source code 
2. by running a custom script to download the latest version of the bootstrapper

The bootstrapper is available as a NuGet package with id Arbor.X.Bootstrapper

# Azure Web sites Kudu support

Arbor.X is aware if Azure Web sites and can determine if is running in a context where Kudu is available. This enables web sites to be deployed with source code triggers and the same build pipeline as running elsewhere.

## Kudu deployment

Create a .deployment file pointing to /build/build.exe

### Using GIT

The code is cloned from the default repository and checked out with the specified branch into a temporary folder and and build actions are performed from that temp directory. This way the code is always clean.

# Test framework integration

Supported test frameworks

* NUnit
* VSTest
* Machine.Specifications

# MSBuild integration

Find latest version of MSBuild installed on the current machine by looking at registry keys.

## Solution builder

Arbor.X will scan for Visual Studio solution files .sln and build the solution with all configuration and platform combinations defined in the solution file.

# NuGet

NuGet package creation from NuGet package specification files .nuspec.
NuGet package restore

# Version Conntrol System file structure

Recommended file structure
* /src - source code
* /build - build related scripts and tools
* /docs - documents other than README, LICENSE

## Recommended ignored files

* [Tt]emp
* _assemblyPatchInfos
* [Aa]rtifacts
* [Bb]uild/*

## Recommended included files

* packages/repositories.config
* optional build/Build.exe 
* optional build/build.bat