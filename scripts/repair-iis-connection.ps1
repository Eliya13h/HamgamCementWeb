# Repair ERR_CONNECTION_REFUSED / IIS not listening
# Run PowerShell as Administrator.
#
# Examples:
#   .\repair-iis-connection.ps1
#   .\repair-iis-connection.ps1 -App Cement
#   .\repair-iis-connection.ps1 -App Transport
#   .\repair-iis-connection.ps1 -App Both

param(
    [ValidateSet("Cement", "Transport", "Both")]
    [string]$App = "Both",

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

Import-Module WebAdministration -ErrorAction Stop

$targets = if ($App -eq "Both") { @("Cement", "Transport") } else { @($App) }

Write-Step "Checking W3SVC / WAS"
$w3 = Get-Service W3SVC -ErrorAction SilentlyContinue
$was = Get-Service WAS -ErrorAction SilentlyContinue
if (-not $w3 -or -not $was) {
    throw "IIS services not found."
}

if ($was.Status -ne "Running") {
    Start-Service WAS
    Write-Ok "WAS started."
}
else {
    Write-Ok "WAS running."
}

if ($w3.Status -ne "Running") {
    Start-Service W3SVC
    Write-Ok "W3SVC started."
}
else {
    Write-Ok "W3SVC running."
}

foreach ($name in $targets) {
    $siteName = $name
    $poolName = $name

    Write-Step "Repairing $name"

    if (-not (Test-Path "IIS:\AppPools\$poolName")) {
        Write-Bad "App pool not found: $poolName"
        continue
    }

    if ((Get-WebAppPoolState -Name $poolName).Value -ne "Started") {
        Start-WebAppPool -Name $poolName
        Write-Ok "App pool started: $poolName"
    }
    else {
        Restart-WebAppPool -Name $poolName
        Write-Ok "App pool restarted: $poolName"
    }

    $site = Get-Website -Name $siteName -ErrorAction SilentlyContinue
    if (-not $site) {
        Write-Bad "Website not found: $siteName"
        continue
    }

    if ($site.State -ne "Started") {
        Start-Website -Name $siteName
        Write-Ok "Website started: $siteName"
    }
    else {
        Write-Ok "Website already started: $siteName"
    }

    $bindings = @(Get-WebBinding -Name $siteName -ErrorAction SilentlyContinue)
    if ($bindings.Count -eq 0) {
        Write-Bad "No bindings on $siteName"
    }
    else {
        $bindings | ForEach-Object { Write-Ok ("Binding: {0} {1}" -f $_.protocol, $_.bindingInformation) }
    }

    Get-Website -Name $siteName |
        Format-Table Name, State, PhysicalPath -AutoSize
}

Write-Step "Port $Port listeners"
try {
    $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($listeners) {
        Write-Ok "Something is listening on port $Port."
    }
    else {
        Write-WarnMsg "Nothing listening on port $Port yet. Wait a few seconds and retry."
    }
}
catch {
    Write-WarnMsg "Could not query listeners: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "Repair finished." -ForegroundColor Green
Write-Host "Test: http://Cement.local/  and  http://Transport.local/" -ForegroundColor Yellow
Write-Host ""
