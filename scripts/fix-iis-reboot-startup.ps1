# Fix 500.30 after Windows reboot (IIS starts before SQL Server)
# Run PowerShell as Administrator.
#
# Examples:
#   .\fix-iis-reboot-startup.ps1
#   .\fix-iis-reboot-startup.ps1 -PoolName "Cement"
#   .\fix-iis-reboot-startup.ps1 -PoolName "Transport"
#   .\fix-iis-reboot-startup.ps1 -PoolName "Both"

param(
    [ValidateSet("Cement", "Transport", "Both")]
    [string]$PoolName = "Both",

    [int]$StartupDelaySeconds = 90
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Green
}

function Write-WarnMsg([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Yellow
}

function Get-SqlServerService {
    $candidates = @(
        (Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue),
        (Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue)
    ) | Where-Object { $_ -ne $null }

    foreach ($service in $candidates) {
        if ($service.StartType -ne "Disabled") {
            return $service
        }
    }

    return $null
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator."
}

Import-Module WebAdministration -ErrorAction Stop

$pools = if ($PoolName -eq "Both") { @("Cement", "Transport") } else { @($PoolName) }

Write-Step "Checking SQL Server service"
$sqlService = Get-SqlServerService
if (-not $sqlService) {
    throw "No SQL Server service found (MSSQLSERVER or MSSQL`$SQLEXPRESS)."
}
Write-Ok "Using SQL service: $($sqlService.Name) ($($sqlService.DisplayName))"

if ($sqlService.StartType -ne "Automatic") {
    Set-Service -Name $sqlService.Name -StartupType Automatic
    Write-Ok "SQL service startup type set to Automatic."
}
else {
    Write-Ok "SQL service is already Automatic."
}

Write-Step "Ensuring W3SVC dependency is correct"
$w3Config = & sc.exe qc W3SVC 2>&1 | Out-String
if ($w3Config -notmatch "DEPENDENCIES\s*:\s*WAS") {
    Write-WarnMsg "W3SVC dependency is not WAS. Restoring default IIS dependency..."
    & sc.exe config W3SVC depend= WAS | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Ok "W3SVC dependency restored to WAS."
    }
    else {
        Write-WarnMsg "Could not restore W3SVC dependency automatically."
    }
}
else {
    Write-Ok "W3SVC dependency is WAS (correct)."
}

foreach ($pool in $pools) {
    Write-Step "Registering delayed restart for app pool: $pool"

    if (-not (Test-Path "IIS:\AppPools\$pool")) {
        Write-WarnMsg "App pool not found yet: $pool (skip scheduled task)."
        continue
    }

    $taskName = "Hamgam-$pool-RestartAppPoolAfterBoot"
    $scriptBlock = @"
Import-Module WebAdministration
Start-Sleep -Seconds $StartupDelaySeconds
if (Test-Path 'IIS:\AppPools\$pool') { Restart-WebAppPool -Name '$pool' }
"@

    $taskAction = New-ScheduledTaskAction `
        -Execute "powershell.exe" `
        -Argument "-NoProfile -ExecutionPolicy Bypass -Command `"$scriptBlock`""

    $taskTrigger = New-ScheduledTaskTrigger -AtStartup
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest
    $taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask `
        -TaskName $taskName `
        -Action $taskAction `
        -Trigger $taskTrigger `
        -Principal $taskPrincipal `
        -Settings $taskSettings `
        -Force | Out-Null

    Write-Ok "Scheduled task registered: $taskName (delay ${StartupDelaySeconds}s after boot)."

    Restart-WebAppPool -Name $pool
    Write-Ok "App pool '$pool' restarted."
}

Write-Host ""
Write-Host "Reboot startup fix applied for: $($pools -join ', ')" -ForegroundColor Green
Write-Host "After reboot, wait about $StartupDelaySeconds seconds before opening the sites." -ForegroundColor Yellow
Write-Host "If connection is refused, run: .\repair-iis-connection.ps1" -ForegroundColor Yellow
Write-Host ""
