# Clean Architecture Code Generator Wrapper Script
# Usage: .\generate.ps1 [entity] [options]

param(
    [string]$Entity = "",
    [switch]$DryRun,
    [switch]$Overwrite,
    [switch]$ActionsOnly,
    [string]$CustomActions = "",
    [switch]$Help
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$GeneratorDir = Join-Path $ScriptDir "tools\CodeGenerator"

# Check if generator exists
if (-not (Test-Path $GeneratorDir)) {
    Write-Host "❌ Error: CodeGenerator not found at $GeneratorDir" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

# Build arguments
$args = @()

if ($Entity) {
    $args += "--entity", $Entity
}

if ($DryRun) {
    $args += "--dry-run"
}

if ($Overwrite) {
    $args += "--overwrite"
}

if ($ActionsOnly) {
    $args += "--actions-only"
}

if ($CustomActions) {
    $args += "--custom-actions", $CustomActions
}

if ($Help) {
    $args += "--help"
}

# Run generator
Write-Host "🚀 Running Code Generator..." -ForegroundColor Cyan

if ($args.Count -eq 0) {
    Write-Host "🔍 Listing available entities..." -ForegroundColor Yellow
    dotnet run --project $GeneratorDir
} else {
    dotnet run --project $GeneratorDir -- $args
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Code generation completed successfully!" -ForegroundColor Green
    Write-Host "💡 Next steps:" -ForegroundColor Cyan
    Write-Host "   1. dotnet build" -ForegroundColor White
    Write-Host "   2. dotnet run --project src\API" -ForegroundColor White
    Write-Host "   3. Open https://localhost:7049/swagger" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "❌ Code generation failed!" -ForegroundColor Red
}