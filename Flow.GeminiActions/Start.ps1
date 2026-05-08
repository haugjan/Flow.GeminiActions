# ===============================
# Build-And-Restart-FlowLauncher.ps1
# Stops Flow Launcher, builds the plugin and restarts Flow Launcher
# ===============================
param (
    [string]$SolutionPath = ".\Flow.GeminiActions.csproj",
    [string]$BuildConfig = "Debug",
    [string]$PluginFolderName,
    [string]$FlowLauncherPath = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"
)

if (-not $PluginFolderName) {
    $manifestPath = Join-Path -Path (Split-Path $SolutionPath) -ChildPath "plugin.json"
    if (-not (Test-Path $manifestPath)) {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "plugin.json"
    }
    $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
    $PluginFolderName = "$($manifest.Name)-$($manifest.Version)"
}

Write-Host "----------------------------------------"
Write-Host "Flow Launcher Plugin Build Script"
Write-Host "----------------------------------------"

Write-Host "Stopping Flow Launcher..."
Get-Process "Flow.Launcher" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Building solution: $SolutionPath ($BuildConfig)"
dotnet build $SolutionPath -c $BuildConfig

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Copying build output to Flow Launcher plugins folder..."
$buildOutput = Join-Path -Path (Split-Path $SolutionPath) -ChildPath "bin\$BuildConfig\net9.0-windows"
$pluginTarget = Join-Path "$env:APPDATA\FlowLauncher\Plugins" $PluginFolderName

if (-not (Test-Path $pluginTarget)) {
    New-Item -ItemType Directory -Path $pluginTarget | Out-Null
}

Copy-Item -Path "$buildOutput\*" -Destination $pluginTarget -Recurse -Force
Write-Host "Files copied to:`n$pluginTarget"

Write-Host "Starting Flow Launcher..."
Start-Process $FlowLauncherPath

Write-Host "----------------------------------------"
Write-Host "Done. Flow Launcher has been restarted."
Write-Host "----------------------------------------"
