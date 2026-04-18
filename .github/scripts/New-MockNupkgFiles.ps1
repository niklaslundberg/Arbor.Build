[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $WindowsOutputDirectory,
    [Parameter(Mandatory = $true)]
    [string] $LinuxOutputDirectory,
    [Parameter(Mandatory = $false)]
    [string] $PackageName = 'Arbor.Build.Tool.1.0.0-test.nupkg'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path $WindowsOutputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $LinuxOutputDirectory | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($outputDir in @($WindowsOutputDirectory, $LinuxOutputDirectory)) {
    $packagePath = Join-Path $outputDir $PackageName
    $zip = [System.IO.Compression.ZipFile]::Open(
        $packagePath,
        [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $entry = $zip.CreateEntry('lib/Arbor.Build.Tool.dll')
        $writer = [System.IO.StreamWriter]::new($entry.Open())
        try { $writer.Write('mock content') } finally { $writer.Dispose() }
    }
    finally { $zip.Dispose() }
    Write-Host "Created: $packagePath"
}
