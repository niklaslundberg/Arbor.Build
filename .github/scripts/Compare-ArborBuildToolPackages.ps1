[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $WindowsPackagePath,
    [Parameter(Mandatory = $true)]
    [string] $LinuxPackagePath,
    [Parameter(Mandatory = $true)]
    [string] $ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$reportDirectory = Split-Path -Parent -Path $ReportPath
if ($reportDirectory -and -not (Test-Path -LiteralPath $reportDirectory)) {
    New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
}

function Write-MissingPackageReport {
    param(
        [string] $WindowsPath,
        [string] $LinuxPath
    )

    Set-Content -Path $ReportPath -Encoding utf8 -Value @(
        'Could not find both Windows and Linux Arbor.Build.Tool packages for comparison'
        "Windows package: $WindowsPath"
        "Linux package: $LinuxPath"
    )
}

if ([string]::IsNullOrWhiteSpace($WindowsPackagePath) -or -not (Test-Path -LiteralPath $WindowsPackagePath)) {
    Write-MissingPackageReport -WindowsPath $WindowsPackagePath -LinuxPath $LinuxPackagePath
    exit 1
}

if ([string]::IsNullOrWhiteSpace($LinuxPackagePath) -or -not (Test-Path -LiteralPath $LinuxPackagePath)) {
    Write-MissingPackageReport -WindowsPath $WindowsPackagePath -LinuxPath $LinuxPackagePath
    exit 1
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-ZipEntryHashes {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackagePath
    )

    $hashes = @{}
    $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)

    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.FullName.EndsWith('/')) {
                continue
            }

            $hasher = [System.Security.Cryptography.SHA256]::Create()

            try {
                $buffer = New-Object byte[] 8192
                $stream = $entry.Open()

                try {
                    while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                        $hasher.TransformBlock($buffer, 0, $read, $null, $null) | Out-Null
                    }

                    $hasher.TransformFinalBlock([byte[]]::new(0), 0, 0) | Out-Null
                    $hashes[$entry.FullName] = ($hasher.Hash | ForEach-Object { $_.ToString('x2') }) -join ''
                }
                finally {
                    $stream.Dispose()
                }
            }
            finally {
                $hasher.Dispose()
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    return $hashes
}

$windowsEntries = Get-ZipEntryHashes -PackagePath $WindowsPackagePath
$linuxEntries = Get-ZipEntryHashes -PackagePath $LinuxPackagePath

$windowsOnly = $windowsEntries.Keys | Where-Object { -not $linuxEntries.ContainsKey($_) } | Sort-Object
$linuxOnly = $linuxEntries.Keys | Where-Object { -not $windowsEntries.ContainsKey($_) } | Sort-Object
$changed = $windowsEntries.Keys | Where-Object { $linuxEntries.ContainsKey($_) -and $windowsEntries[$_] -ne $linuxEntries[$_] } | Sort-Object
$equivalent = -not $windowsOnly -and -not $linuxOnly -and -not $changed

$reportLines = @(
    '# Arbor.Build.Tool package comparison (Windows vs Linux)'
    ''
    "- Windows package: ``$WindowsPackagePath``"
    "- Linux package: ``$LinuxPackagePath``"
    "- Equivalent package contents: ``$(if ($equivalent) { 'yes' } else { 'no' })``"
    ''
)

if ($equivalent) {
    $reportLines += 'Packages contain the same files with identical content hashes.'
}
else {
    if ($windowsOnly) {
        $reportLines += '## Files only in Windows package'
        $reportLines += ($windowsOnly | ForEach-Object { "- ``$_``" })
        $reportLines += ''
    }

    if ($linuxOnly) {
        $reportLines += '## Files only in Linux package'
        $reportLines += ($linuxOnly | ForEach-Object { "- ``$_``" })
        $reportLines += ''
    }

    if ($changed) {
        $reportLines += '## Files with different content'
        foreach ($item in $changed) {
            $reportLines += "- ``$item``"
            $reportLines += "  - Windows SHA256: ``$($windowsEntries[$item])``"
            $reportLines += "  - Linux SHA256: ``$($linuxEntries[$item])``"
        }
        $reportLines += ''
    }
}

Set-Content -Path $ReportPath -Encoding utf8 -Value $reportLines

# Emit the full report to stdout so the comparison result is visible in CI logs
# without having to download the uploaded artifact.
Write-Host '--- Arbor.Build.Tool package comparison report ---'
$reportLines | ForEach-Object { Write-Host $_ }
Write-Host '--- end of report ---'

if ($equivalent) {
    Write-Host 'Windows and Linux Arbor.Build.Tool packages are equivalent.'
}
else {
    Write-Warning 'Windows and Linux Arbor.Build.Tool packages differ. See report for details.'
}

# Exit 0 on a successful comparison regardless of whether the packages are
# equivalent. The report captures the outcome and is uploaded as a CI artifact;
# differences between Windows and Linux builds are expected (psmdcp GUIDs,
# nuspec metadata, per-OS binary output) and should not fail CI. The earlier
# `exit 1` paths above still run when the comparison cannot be performed
# (missing or unreadable package).
