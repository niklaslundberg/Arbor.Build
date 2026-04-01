#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test Explorer Diagnostic and Reset Script
    
.DESCRIPTION
    Diagnoses and fixes Test Explorer issues by:
    1. Checking test framework installations
    2. Validating .runsettings configuration
    3. Clearing Visual Studio cache
    4. Rebuilding projects
    5. Verifying test discovery
    
.EXAMPLE
    .\Test-ExplorerReset.ps1 -Verbose
    
.PARAMETER Force
    Forces a clean rebuild and cache reset
    
.PARAMETER Verbose
    Shows detailed diagnostic output
#>

param(
    [switch]$Force,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $color = @{
        "INFO"    = "Cyan"
        "SUCCESS" = "Green"
        "WARNING" = "Yellow"
        "ERROR"   = "Red"
    }[$Status]
    Write-Host "[$Status]" -ForegroundColor $color -NoNewline
    Write-Host " $Message"
}

function Test-RunsettingsFile {
    Write-Status "Checking .runsettings configuration..."
    
    $runSettingsPath = "$(Get-Location)\.runsettings"
    
    if (-not (Test-Path $runSettingsPath)) {
        Write-Status "❌ .runsettings not found at: $runSettingsPath" -Status "ERROR"
        return $false
    }
    
    Write-Status "✅ .runsettings found" -Status "SUCCESS"
    
    # Validate XML
    try {
        $xml = [xml](Get-Content $runSettingsPath)
        Write-Status "✅ .runsettings is valid XML" -Status "SUCCESS"
        
        # Check for test framework configurations
        $frameworks = @("xUnit", "MSTest", "NUnit", "MSpec")
        foreach ($fw in $frameworks) {
            if ($xml.RunSettings.InnerXml -match $fw) {
                Write-Status "✅ $fw configuration found" -Status "SUCCESS"
            } else {
                Write-Status "⚠️  $fw configuration not found" -Status "WARNING"
            }
        }
        
        return $true
    }
    catch {
        Write-Status "❌ Invalid .runsettings XML: $_" -Status "ERROR"
        return $false
    }
}

function Test-TestFrameworks {
    Write-Status "Checking test framework packages..."
    
    # Check for test project files
    $testProjects = @(
        "tests\Arbor.Build.Tests.Unit\Arbor.Build.Tests.Unit.csproj",
        "tests\Arbor.Build.Tests.Integration\Arbor.Build.Tests.Integration.csproj"
    )
    
    foreach ($project in $testProjects) {
        if (-not (Test-Path $project)) {
            Write-Status "⚠️  Project not found: $project" -Status "WARNING"
            continue
        }
        
        $content = Get-Content $project -Raw
        $projectName = Split-Path $project -Leaf
        
        $packages = @(
            @{ Name = "xunit"; Pattern = "xunit" }
            @{ Name = "MSTest"; Pattern = "Microsoft.NET.Test.Sdk" }
            @{ Name = "Machine.Specifications"; Pattern = "Machine.Specifications" }
        )
        
        foreach ($pkg in $packages) {
            if ($content -match $pkg.Pattern) {
                Write-Status "✅ $($pkg.Name) in $projectName" -Status "SUCCESS"
            }
        }
    }
}

function Clear-VisualStudioCache {
    Write-Status "Clearing Visual Studio cache..."
    
    $cachePaths = @(
        "$env:LOCALAPPDATA\Microsoft\VisualStudio\*",
        "$env:LOCALAPPDATA\Microsoft\VisualStudio.Managed\*"
    )
    
    foreach ($path in $cachePaths) {
        $dirs = Get-ChildItem $path -ErrorAction SilentlyContinue
        if ($dirs) {
            # Remove test-related cache
            Get-ChildItem $path -Filter "*cache*" -Recurse -Force -ErrorAction SilentlyContinue | 
                Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            Write-Status "✅ Cleared cache at $path" -Status "SUCCESS"
        }
    }
}

function Test-Build {
    Write-Status "Building solution..."
    
    if ($Force) {
        Write-Status "🔄 Performing clean build..."
        & dotnet clean --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Status "⚠️  Clean failed but continuing..." -Status "WARNING"
        }
    }
    
    & dotnet build --nologo
    if ($LASTEXITCODE -eq 0) {
        Write-Status "✅ Build successful" -Status "SUCCESS"
        return $true
    }
    else {
        Write-Status "❌ Build failed - fix errors before retrying" -Status "ERROR"
        return $false
    }
}

function Test-TestDiscovery {
    Write-Status "Checking test discovery..."
    
    $testAssemblies = Get-ChildItem -Path "tests\*\bin\Debug\net*\*.Tests.*.dll" -ErrorAction SilentlyContinue
    
    if ($testAssemblies.Count -eq 0) {
        Write-Status "⚠️  No test assemblies found (build first)" -Status "WARNING"
        return $false
    }
    
    Write-Status "✅ Found $($testAssemblies.Count) test assemblies" -Status "SUCCESS"
    foreach ($asm in $testAssemblies) {
        Write-Host "   📦 $($asm.Name)"
    }
    
    return $true
}

function Get-Summary {
    Write-Host ""
    Write-Status "═════════════════════════════════════════" -Status "INFO"
    Write-Status "Test Explorer Configuration Summary" -Status "INFO"
    Write-Status "═════════════════════════════════════════" -Status "INFO"
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "1. Close Visual Studio completely"
    Write-Host "2. Run this script with -Force flag to do a clean build"
    Write-Host "3. Reopen Visual Studio"
    Write-Host "4. Open Test Explorer (Ctrl+E, T)"
    Write-Host "5. Click Refresh (↻) button"
    Write-Host "6. Wait 30 seconds for test discovery"
    Write-Host ""
    Write-Host "If tests still don't appear:"
    Write-Host "• Test → Configure Run Settings → Select .runsettings"
    Write-Host "• Verify .runsettings has correct framework configurations"
    Write-Host "• Run this script again with -Force flag"
    Write-Host ""
}

# Main execution
Write-Host ""
Write-Status "═════════════════════════════════════════" -Status "INFO"
Write-Status "Test Explorer Diagnostic Tool" -Status "INFO"
Write-Status "═════════════════════════════════════════" -Status "INFO"
Write-Host ""

# Run diagnostics
Test-RunsettingsFile
Write-Host ""

Test-TestFrameworks
Write-Host ""

if ($Force) {
    Clear-VisualStudioCache
    Write-Host ""
}

if (Test-Build) {
    Write-Host ""
    Test-TestDiscovery
}

Write-Host ""
Get-Summary
