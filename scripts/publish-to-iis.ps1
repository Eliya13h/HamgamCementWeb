# Publish Cement/Transport and copy output to C:\inetpub\Cement or C:\inetpub\Transport
# Run PowerShell as Administrator (needed to write under C:\inetpub)
#
# Examples:
#   .\publish-to-iis.ps1 -App Cement
#   .\publish-to-iis.ps1 -App Transport
#   .\publish-to-iis.ps1 -App Both

param(
    [ValidateSet("Cement", "Transport", "Both")]
    [string]$App = "Both",

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator (needed to write under C:\inetpub)."
}

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$apps = @{
    Cement = @{
        Project = Join-Path $root "HamgamCementWeb.Server\HamgamCementWeb.Server.csproj"
        Dest    = "C:\inetpub\Cement"
        Dll     = "HamgamCementWeb.Server.dll"
    }
    Transport = @{
        Project = Join-Path $root "HamgamTransport.Server\HamgamTransport.Server.csproj"
        Dest    = "C:\inetpub\Transport"
        Dll     = "HamgamTransport.Server.dll"
    }
}

$targets = if ($App -eq "Both") { @("Cement", "Transport") } else { @($App) }

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Green
}

foreach ($name in $targets) {
    $cfg = $apps[$name]
    $tempOut = Join-Path $env:TEMP ("HamgamPublish_" + $name)

    Write-Step "Publishing $name"
    if (-not (Test-Path $cfg.Project)) {
        throw "Project not found: $($cfg.Project)"
    }

    if (Test-Path $tempOut) {
        Remove-Item -Path $tempOut -Recurse -Force
    }

    & dotnet publish $cfg.Project -c $Configuration -o $tempOut
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $name"
    }

    $dllPath = Join-Path $tempOut $cfg.Dll
    if (-not (Test-Path $dllPath)) {
        throw "Publish succeeded but DLL missing: $dllPath"
    }

    Write-Step "Copying to $($cfg.Dest)"
    New-Item -ItemType Directory -Force -Path $cfg.Dest | Out-Null

    # Stop site/pool if present so files are not locked
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    if (Get-Command Get-Website -ErrorAction SilentlyContinue) {
        $site = Get-Website -Name $name -ErrorAction SilentlyContinue
        if ($site) {
            try { Stop-Website -Name $name -ErrorAction SilentlyContinue } catch { }
        }
        if (Test-Path "IIS:\AppPools\$name") {
            try { Stop-WebAppPool -Name $name -ErrorAction SilentlyContinue } catch { }
            Start-Sleep -Seconds 1
        }
    }

    # Keep logs/uploads if they already exist
    Get-ChildItem -Path $cfg.Dest -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notin @("logs", "wwwroot") } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    if (Test-Path (Join-Path $cfg.Dest "wwwroot")) {
        Get-ChildItem -Path (Join-Path $cfg.Dest "wwwroot") -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ne "uploads" } |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    Copy-Item -Path (Join-Path $tempOut "*") -Destination $cfg.Dest -Recurse -Force

    New-Item -ItemType Directory -Force -Path (Join-Path $cfg.Dest "logs") | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $cfg.Dest "wwwroot\uploads") | Out-Null

    Write-Ok "Published: $($cfg.Dest)\$($cfg.Dll)"

    if (Get-Command Start-WebAppPool -ErrorAction SilentlyContinue) {
        if (Test-Path "IIS:\AppPools\$name") {
            try { Start-WebAppPool -Name $name -ErrorAction SilentlyContinue } catch { }
        }
        $site = Get-Website -Name $name -ErrorAction SilentlyContinue
        if ($site) {
            try { Start-Website -Name $name -ErrorAction SilentlyContinue } catch { }
        }
    }
}

Write-Host ""
Write-Host "Publish completed." -ForegroundColor Green
Write-Host "Next (if sites not created yet):" -ForegroundColor Yellow
foreach ($name in $targets) {
    Write-Host "  .\setup-iis.ps1 -App $name"
}
Write-Host ""
