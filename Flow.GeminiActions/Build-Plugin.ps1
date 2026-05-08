# Build and package Flow Launcher plugin
# This script builds the plugin and creates a ZIP file ready for Flow Launcher installation

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\dist"
)

$ErrorActionPreference = "Stop"

$PluginManifest = Get-Content -Raw "plugin.json" | ConvertFrom-Json
$PluginVersion = $PluginManifest.Version
$PluginName = "Flow.GeminiActions"

Write-Host "Building Flow Launcher Plugin: $PluginName" -ForegroundColor Green

$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(".\dist")
}

if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

Write-Host "Building project in $Configuration configuration..." -ForegroundColor Yellow
dotnet build "Flow.GeminiActions.csproj" --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

$BinPath = "bin\$Configuration\net9.0-windows"
$TempPluginDir = Join-Path $OutputPath "temp\$PluginName"

New-Item -ItemType Directory -Path $TempPluginDir -Force | Out-Null

Write-Host "Copying plugin files..." -ForegroundColor Yellow

Copy-Item "$BinPath\Flow.GeminiActions.dll" -Destination $TempPluginDir
Copy-Item "$BinPath\Flow.GeminiActions.deps.json" -Destination $TempPluginDir -ErrorAction SilentlyContinue

Copy-Item "plugin.json" -Destination $TempPluginDir

if (Test-Path "Images") {
    Copy-Item "Images" -Destination $TempPluginDir -Recurse
}

$ExcludePatterns = @(
    "Microsoft.WindowsDesktop.App.*",
    "Microsoft.NETCore.App.*",
    "System.*",
    "netstandard.*",
    "mscorlib.*",
    "WindowsBase.*",
    "PresentationCore.*",
    "PresentationFramework.*"
)

Get-ChildItem "$BinPath\*.dll" | Where-Object {
    $fileName = $_.Name
    $exclude = $false
    foreach ($pattern in $ExcludePatterns) {
        if ($fileName -like $pattern) {
            $exclude = $true
            break
        }
    }
    return !$exclude -and $fileName -ne "Flow.GeminiActions.dll"
} | ForEach-Object {
    Copy-Item $_.FullName -Destination $TempPluginDir
    Write-Host "  Copied dependency: $($_.Name)" -ForegroundColor Gray
}

$ZipFileName = "$PluginName-v$PluginVersion.zip"
$ZipPath = Join-Path $OutputPath $ZipFileName

Write-Host "Creating ZIP package: $ZipFileName" -ForegroundColor Yellow

$ZipDir = Split-Path $ZipPath -Parent
if (-not (Test-Path $ZipDir)) {
    New-Item -ItemType Directory -Path $ZipDir -Force | Out-Null
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($TempPluginDir, $ZipPath)

Remove-Item (Join-Path $OutputPath "temp") -Recurse -Force

Write-Host "ZIP package created successfully!" -ForegroundColor Green
Write-Host "Package location: $ZipPath" -ForegroundColor Cyan
