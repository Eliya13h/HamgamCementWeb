# Repair ERR_CONNECTION_REFUSED / IIS not listening after fix-iis-reboot-startup.ps1
# Run PowerShell as Administrator on the destination machine.
#
# Examples:
#   .\repair-iis-connection.ps1
#   .\repair-iis-connection.ps1 -SiteName "HamgamCementWeb" -PoolName "HamgamCementWeb" -Port 80

param(
    [string]$SiteName = "HamgamCementWeb",
    [string]$PoolName = "HamgamCementWeb",
    [int]$Port = 80
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Green
}

function Write-Bad([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Red
}

function Write-WarnMsg([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Yellow
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator."
}

Write-Step "Restoring W3SVC dependency (must be WAS, not SQL)"
& sc.exe config W3SVC depend= WAS | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Ok "W3SVC dependency restored to WAS."
}
else {
    Write-WarnMsg "Could not restore W3SVC dependency automatically."
}

Write-Step "Starting IIS services"
foreach ($serviceName in @("HTTP", "WAS", "W3SVC")) {
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if (-not $service) {
        Write-WarnMsg "Service not found: $serviceName"
        continue
    }

    if ($service.Status -ne "Running") {
        Start-Service -Name $serviceName
        Write-Ok "$serviceName started."
    }
    else {
        Write-Ok "$serviceName already running."
    }
}

Import-Module WebAdministration -ErrorAction Stop

Write-Step "Starting website and app pool"
if (-not (Test-Path "IIS:\AppPools\$PoolName")) {
    Write-Bad "App pool not found: $PoolName"
}
else {
    if ((Get-WebAppPoolState -Name $PoolName).Value -ne "Started") {
        Start-WebAppPool -Name $PoolName
        Write-Ok "App pool started: $PoolName"
    }
    else {
        Restart-WebAppPool -Name $PoolName
        Write-Ok "App pool restarted: $PoolName"
    }
}

$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
    Write-Bad "Website not found: $SiteName"
    Write-WarnMsg "Run setup-iis.ps1 first."
}
else {
    if ($site.State -ne "Started") {
        Start-Website -Name $SiteName
        Write-Ok "Website started: $SiteName"
    }
    else {
        Write-Ok "Website already started: $SiteName"
    }

    $bindings = @(Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue)
    if ($bindings.Count -eq 0) {
        Write-Bad "Website has no HTTP bindings."
    }
    else {
        $bindings | ForEach-Object {
            Write-Ok "Binding: $($_.protocol) $($_.bindingInformation)"
        }
    }
}

Write-Step "Checking port $Port"
$listening = netstat -ano | Select-String ":$Port\s"
if ($listening) {
    Write-Ok "Something is listening on port $Port."
    $listening | Select-Object -First 3 | ForEach-Object { Write-Host "        $_" }
}
else {
    Write-Bad "Nothing is listening on port $Port."
    Write-WarnMsg "IIS may still be failing to start. Check Event Viewer > Windows Logs > System."
}

Write-Step "Site status"
Get-Website -Name $SiteName -ErrorAction SilentlyContinue |
    Format-Table Name, State, PhysicalPath -AutoSize

Write-Host ""
Write-Host "Try opening:" -ForegroundColor Green
Write-Host "  http://localhost:$Port" -ForegroundColor Green
Write-Host ""
Write-Host "If you still get connection refused, send output of:" -ForegroundColor Yellow
Write-Host "  Get-Service W3SVC,WAS,HTTP | Format-Table Name,Status,StartType"
Write-Host "  sc.exe qc W3SVC"
Write-Host ""
