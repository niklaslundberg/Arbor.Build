# WSL Test Runner - PowerShell Wrapper
# Runs all tests in WSL Ubuntu with proper environment setup

param(
    [string]$Filter = "",
    [string]$Configuration = "Debug",
    [switch]$Verbose = $false
)

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "         WSL UBUNTU TEST EXECUTION                    " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

# Get workspace root
$workspaceRoot = "E:\N\Arbor.Build"
$wslPath = "/mnt/e/N/Arbor.Build"

# Construct dotnet test command
$testArgs = "test tests/Arbor.Build.Tests.Integration --configuration $Configuration"

if ($Filter) {
    $testArgs += " --filter '$Filter'"
}

$verbosity = if ($Verbose) { "normal" } else { "minimal" }
$testArgs += " --verbosity $verbosity"

Write-Host "Workspace: $workspaceRoot" -ForegroundColor Yellow
Write-Host "Test Command: dotnet test tests/Arbor.Build.Tests.Integration --configuration $Configuration" -ForegroundColor Yellow
Write-Host "Environment: WSL Ubuntu" -ForegroundColor Yellow
Write-Host ""

# Run tests in WSL
Write-Host "Running tests..." -ForegroundColor Green
Write-Host ""

$command = "cd $wslPath && /home/niklas/.dotnet/dotnet $testArgs"
wsl --distribution Ubuntu -- bash -c $command

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
if ($exitCode -eq 0) {
    Write-Host "  ALL TESTS PASSED" -ForegroundColor Green
}
else {
    Write-Host "  SOME TESTS FAILED (Exit Code: $exitCode)" -ForegroundColor Red
}
Write-Host "=======================================================" -ForegroundColor Cyan

exit $exitCode
