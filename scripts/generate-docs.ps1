# TextDiff.Sharp Documentation Generation Script

param(
    [switch]$Serve,
    [switch]$Clean,
    [string]$Output = "_site"
)

$ErrorActionPreference = "Stop"

Write-Host "TextDiff.Sharp Documentation Generator" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Check if DocFX is installed
try {
    $docfxVersion = docfx --version
    Write-Host "Using DocFX version: $docfxVersion" -ForegroundColor Cyan
} catch {
    Write-Host "DocFX not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g docfx
}

# Navigate to docs directory
$docsPath = Join-Path $PSScriptRoot "..\docs"
if (-not (Test-Path $docsPath)) {
    Write-Error "Documentation directory not found: $docsPath"
    exit 1
}

Set-Location $docsPath

# Clean previous builds if requested
if ($Clean) {
    Write-Host "Cleaning previous documentation build..." -ForegroundColor Yellow
    if (Test-Path $Output) {
        Remove-Item $Output -Recurse -Force
    }
    if (Test-Path "api") {
        Remove-Item "api" -Recurse -Force
    }
    if (Test-Path "obj") {
        Remove-Item "obj" -Recurse -Force
    }
}

# Build documentation
Write-Host "Building documentation..." -ForegroundColor Cyan

try {
    # Generate metadata
    Write-Host "Generating API metadata..." -ForegroundColor Yellow
    docfx metadata docfx.json

    # Build documentation site
    Write-Host "Building documentation site..." -ForegroundColor Yellow
    docfx build docfx.json

    Write-Host "Documentation built successfully!" -ForegroundColor Green
    Write-Host "Output location: $((Get-Item $Output).FullName)" -ForegroundColor Cyan

    # Serve documentation if requested
    if ($Serve) {
        Write-Host "Starting documentation server..." -ForegroundColor Cyan
        Write-Host "Documentation will be available at: http://localhost:8080" -ForegroundColor Yellow
        Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
        docfx serve $Output
    }
} catch {
    Write-Error "Documentation generation failed: $_"
    exit 1
}

Write-Host "Documentation generation completed!" -ForegroundColor Green