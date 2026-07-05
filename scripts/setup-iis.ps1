# IIS setup for HamgamCementWeb
# Prerequisite: copy dotnet publish output to C:\inetpub\HamgamCementWeb
# Run PowerShell as Administrator
#
# Examples:
#   .\setup-iis.ps1
#   .\setup-iis.ps1 -Port 5085
#   .\setup-iis.ps1 -PublishPath "C:\inetpub\HamgamCementWeb"

param(
    [string]$SiteName = "HamgamCementWeb",
    [string]$PoolName = "HamgamCementWeb",
    [string]$PublishPath = "C:\inetpub\HamgamCementWeb",
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

function Write-WarnMsg([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Yellow
}

function Write-Err([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Red
}

function Invoke-IisCommand {
    param(
        [scriptblock]$Action,
        [string]$WarningMessage
    )
    try {
        & $Action
        return $true
    }
    catch {
        Write-WarnMsg "$WarningMessage ($($_.Exception.Message))"
        return $false
    }
}

function Disable-DefaultWebSitePort {
    param([int]$HttpPort)

    $appcmd = Join-Path $env:SystemRoot "System32\inetsrv\appcmd.exe"
    $defaultSite = "Default Web Site"

    Invoke-IisCommand { Stop-Website -Name $defaultSite } "Could not stop $defaultSite" | Out-Null

    $removed = Invoke-IisCommand {
        Remove-WebBinding -Name $defaultSite -Protocol "http" -BindingInformation "*:${HttpPort}:"
    } "Could not remove binding from $defaultSite via PowerShell"

    if (-not $removed -and (Test-Path $appcmd)) {
        & $appcmd set site $defaultSite "/-bindings.[protocol='http',bindingInformation='*:${HttpPort}:']" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "Port $HttpPort removed from $defaultSite via appcmd."
        }
        else {
            Write-WarnMsg "Default Web Site may still use port $HttpPort. Ignore if HamgamCementWeb works."
        }
    }
}

function Start-WebsiteSafe {
    param(
        [string]$Name,
        [string]$PhysicalPath,
        [string]$ApplicationPool,
        [int]$HttpPort
    )

    if (Invoke-IisCommand { Start-Website -Name $Name } "Start-Website failed for $Name") {
        return
    }

    Write-WarnMsg "Recreating site $Name as last resort."
    Invoke-IisCommand { Stop-Website -Name $Name } "Stop before recreate" | Out-Null
    Invoke-IisCommand { Remove-Website -Name $Name } "Remove before recreate" | Out-Null
    New-Website -Name $Name -PhysicalPath $PhysicalPath -Port $HttpPort -ApplicationPool $ApplicationPool | Out-Null
    Start-Website -Name $Name
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator."
}

Write-Step "Checking IIS"
$iisFeature = Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -ErrorAction SilentlyContinue
if (-not $iisFeature -or $iisFeature.State -ne "Enabled") {
    Write-WarnMsg "IIS is not installed. Enable Web Server (IIS) from Windows Features."
    throw "IIS not found."
}
Write-Ok "IIS is enabled."

Import-Module WebAdministration

Write-Step "Checking ASP.NET Core Hosting Bundle"
$hostingBundle = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (-not (Test-Path $hostingBundle)) {
    Write-Err "ASP.NET Core Hosting Bundle 9.0 is not installed."
    Write-WarnMsg "Download: https://dotnet.microsoft.com/download/dotnet/9.0"
    Write-WarnMsg "Install: Hosting Bundle (not SDK or Runtime only)"
    Write-WarnMsg "After install run: iisreset"
    throw "Hosting Bundle not found."
}
Write-Ok "Hosting Bundle is installed."

Write-Step "Checking publish folder"
if (-not (Test-Path $PublishPath)) {
    throw "Publish folder not found: $PublishPath. Copy dotnet publish output there first."
}

$requiredFiles = @(
    "HamgamCementWeb.Server.dll",
    "web.config",
    "appsettings.json"
)
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $PublishPath $file
    if (-not (Test-Path $fullPath)) {
        throw "Required file not found: $fullPath"
    }
}
Write-Ok "Main publish files are present."

if (-not (Test-Path (Join-Path $PublishPath "wwwroot\index.html"))) {
    Write-WarnMsg "wwwroot\index.html not found. Frontend may be missing from publish."
}

Write-Step "Creating Application Pool: $PoolName"
if (-not (Test-Path "IIS:\AppPools\$PoolName")) {
    New-WebAppPool -Name $PoolName | Out-Null
    Write-Ok "Application Pool created."
}
else {
    Write-Ok "Application Pool already exists."
}

Set-ItemProperty "IIS:\AppPools\$PoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name managedPipelineMode -Value "Integrated"
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name startMode -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$PoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
Write-Ok "Application Pool configured (No Managed Code)."

Write-Step "Setting folder permissions"
$folders = @(
    (Join-Path $PublishPath "logs"),
    (Join-Path $PublishPath "wwwroot\uploads"),
    (Join-Path $PublishPath "wwwroot\uploads\company-logo")
)
foreach ($folder in $folders) {
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
}

$poolIdentity = "IIS AppPool\$PoolName"
icacls $PublishPath /grant "${poolIdentity}:(OI)(CI)RX" /T | Out-Null
icacls (Join-Path $PublishPath "logs") /grant "${poolIdentity}:(OI)(CI)M" /T | Out-Null
icacls (Join-Path $PublishPath "wwwroot\uploads") /grant "${poolIdentity}:(OI)(CI)M" /T | Out-Null
Write-Ok "Permissions set for $poolIdentity."

Write-Step "Creating/updating Website: $SiteName"

function Ensure-WebsiteBinding {
    param([string]$Name, [int]$HttpPort)

    $bindings = @(Get-WebBinding -Name $Name -ErrorAction SilentlyContinue)
    $hasPort = $bindings | Where-Object { $_.bindingInformation -like "*:${HttpPort}:*" }
    if (-not $hasPort) {
        if ($bindings.Count -gt 0) {
            foreach ($binding in $bindings) {
                Remove-WebBinding -Name $Name -Protocol $binding.protocol -BindingInformation $binding.bindingInformation -ErrorAction SilentlyContinue
            }
        }
        New-WebBinding -Name $Name -Protocol "http" -Port $HttpPort -IPAddress "*" | Out-Null
        Write-Ok "HTTP binding added: *:${HttpPort}:"
    }
}

$existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
$bindings = if ($existingSite) { @(Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue) } else { @() }

if (-not $existingSite) {
    New-Website -Name $SiteName -PhysicalPath $PublishPath -Port $Port -ApplicationPool $PoolName | Out-Null
    Write-Ok "Website created."
}
elseif ($bindings.Count -eq 0) {
    Write-WarnMsg "Site exists but has no bindings. Recreating site."
    Invoke-IisCommand { Stop-Website -Name $SiteName } "Stop before recreate" | Out-Null
    Remove-Website -Name $SiteName
    New-Website -Name $SiteName -PhysicalPath $PublishPath -Port $Port -ApplicationPool $PoolName | Out-Null
    Write-Ok "Website recreated with binding."
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PublishPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $PoolName
    Ensure-WebsiteBinding -Name $SiteName -HttpPort $Port
    Write-Ok "Website updated."
}

$bindings = @(Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue)
if ($bindings.Count -eq 0) {
    throw "Website still has no bindings. Run: Remove-Website -Name $SiteName then re-run this script."
}

Disable-DefaultWebSitePort -HttpPort $Port
Invoke-IisCommand {
    Remove-WebBinding -Name $SiteName -Protocol "https" -BindingInformation "*:443:"
} "Could not remove HTTPS binding" | Out-Null

Start-WebsiteSafe -Name $SiteName -PhysicalPath $PublishPath -ApplicationPool $PoolName -HttpPort $Port
Restart-WebAppPool -Name $PoolName
Write-Ok "$SiteName started on port $Port."

Write-Step "Site status"
Get-Website -Name $SiteName | Format-Table Name, State, PhysicalPath -AutoSize
Get-WebBinding -Name $SiteName | Format-Table protocol, bindingInformation -AutoSize

Write-Step "Firewall rule (port $Port)"
$ruleName = "HamgamCementWeb HTTP $Port"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    Write-Ok "Firewall rule created."
}
else {
    Write-Ok "Firewall rule already exists."
}

Write-Step "Access URLs"
$ip = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.PrefixOrigin -ne "WellKnown" } |
    Select-Object -First 1 -ExpandProperty IPAddress

Write-Host ""
Write-Host "IIS setup completed successfully." -ForegroundColor Green
Write-Host "  Local:   http://localhost:$Port" -ForegroundColor Green
if ($ip) {
    Write-Host "  Network: http://${ip}:$Port" -ForegroundColor Green
}
Write-Host ""
Write-Host "Reminders:" -ForegroundColor Yellow
Write-Host "  1. SQL Server and database HamgamNimroz must be ready."
Write-Host "  2. Check appsettings.json connection string on this machine."
Write-Host "  3. Grant SQL access to: $poolIdentity"
Write-Host "  4. First login: admin / admin"
Write-Host "  5. On error check: $PublishPath\logs\stdout_*.log"
Write-Host "  6. After reboot 500.30: run scripts\fix-iis-reboot-startup.ps1"
Write-Host ""

$fixScript = Join-Path $PSScriptRoot "fix-iis-reboot-startup.ps1"
if (Test-Path $fixScript) {
    Write-Step "Applying reboot startup fix"
    & $fixScript -PoolName $PoolName
}
