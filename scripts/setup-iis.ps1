# IIS setup for Hamgam apps (Cement / Transport) with hostnames
# Prerequisite: copy dotnet publish output to the PublishPath folder
# Run PowerShell as Administrator
#
# Examples:
#   .\publish-to-iis.ps1 -App Cement     # first: publish + copy to C:\inetpub\Cement
#   .\setup-iis.ps1 -App Cement
#   .\setup-iis.ps1 -App Transport
#   .\setup-iis.ps1 -App Cement -HostName "Cement.local" -Port 80

param(
    [ValidateSet("Cement", "Transport")]
    [string]$App = "Cement",

    [string]$SiteName,
    [string]$PoolName,
    [string]$PublishPath,
    [string]$AppDll,
    [string]$DatabaseName,
    [string]$HostName,
    [int]$Port = 80,
    [string]$LoginHint,
    [switch]$SkipHostsFile
)

$ErrorActionPreference = "Stop"

$defaults = @{
    Cement = @{
        SiteName     = "Cement"
        PoolName     = "Cement"
        PublishPath  = "C:\inetpub\Cement"
        AppDll       = "HamgamCementWeb.Server.dll"
        DatabaseName = "HamgamNimroz"
        HostName     = "Cement.local"
        LoginHint    = "admin / admin"
    }
    Transport = @{
        SiteName     = "Transport"
        PoolName     = "Transport"
        PublishPath  = "C:\inetpub\Transport"
        AppDll       = "HamgamTransport.Server.dll"
        DatabaseName = "HamgamTransport"
        HostName     = "Transport.local"
        LoginHint    = "admin / Admin@123"
    }
}

$cfg = $defaults[$App]
if (-not $SiteName)     { $SiteName = $cfg.SiteName }
if (-not $PoolName)     { $PoolName = $cfg.PoolName }
if (-not $PublishPath)  { $PublishPath = $cfg.PublishPath }
if (-not $AppDll)       { $AppDll = $cfg.AppDll }
if (-not $DatabaseName) { $DatabaseName = $cfg.DatabaseName }
if (-not $HostName)     { $HostName = $cfg.HostName }
if (-not $LoginHint)    { $LoginHint = $cfg.LoginHint }

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

function Get-PrimaryIPv4 {
    $ip = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.PrefixOrigin -ne "WellKnown" -and
            $_.IPAddress -notlike "169.254.*"
        } |
        Sort-Object InterfaceMetric |
        Select-Object -First 1 -ExpandProperty IPAddress
    return $ip
}

function Set-HostsEntry {
    param(
        [string]$Name,
        [string]$IpAddress
    )

    $hostsPath = Join-Path $env:SystemRoot "System32\drivers\etc\hosts"
    $lines = @(Get-Content -Path $hostsPath -ErrorAction Stop)
    $pattern = "^\s*\d{1,3}(\.\d{1,3}){3}\s+$([regex]::Escape($Name))(\s|$)"
    $newLine = "$IpAddress`t$Name"
    $updated = $false
    $result = foreach ($line in $lines) {
        if ($line -match $pattern) {
            $updated = $true
            $newLine
        }
        else {
            $line
        }
    }

    if (-not $updated) {
        $result += ""
        $result += $newLine
    }

    Set-Content -Path $hostsPath -Value $result -Encoding ascii
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
            Write-WarnMsg "Default Web Site may still use port $HttpPort. Ignore if $SiteName works."
        }
    }
}

function Set-HostnameBinding {
    param(
        [string]$Name,
        [int]$HttpPort,
        [string]$HostHeader
    )

    $desired = "*:${HttpPort}:${HostHeader}"
    $bindings = @(Get-WebBinding -Name $Name -ErrorAction SilentlyContinue)

    $hasDesired = $bindings | Where-Object {
        $_.protocol -eq "http" -and $_.bindingInformation -eq $desired
    }

    foreach ($binding in $bindings) {
        $info = $binding.bindingInformation
        $keep = ($binding.protocol -eq "http" -and $info -eq $desired)
        if (-not $keep) {
            Remove-WebBinding -Name $Name -Protocol $binding.protocol -BindingInformation $info -ErrorAction SilentlyContinue
        }
    }

    if (-not $hasDesired) {
        New-WebBinding -Name $Name -Protocol "http" -Port $HttpPort -IPAddress "*" -HostHeader $HostHeader | Out-Null
        Write-Ok "HTTP binding set: $desired"
    }
    else {
        Write-Ok "HTTP binding already correct: $desired"
    }
}

function Start-WebsiteSafe {
    param(
        [string]$Name,
        [string]$PhysicalPath,
        [string]$ApplicationPool,
        [int]$HttpPort,
        [string]$HostHeader
    )

    if (Invoke-IisCommand { Start-Website -Name $Name } "Start-Website failed for $Name") {
        return
    }

    Write-WarnMsg "Recreating site $Name as last resort."
    Invoke-IisCommand { Stop-Website -Name $Name } "Stop before recreate" | Out-Null
    Invoke-IisCommand { Remove-Website -Name $Name } "Remove before recreate" | Out-Null
    New-Website -Name $Name -PhysicalPath $PhysicalPath -Port $HttpPort -HostHeader $HostHeader -ApplicationPool $ApplicationPool | Out-Null
    Start-Website -Name $Name
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator."
}

Write-Step "App profile: $App"
Write-Ok "Site=$SiteName | Pool=$PoolName | Host=$HostName | Port=$Port | Path=$PublishPath"

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
    throw @"
Publish folder not found: $PublishPath

First publish and copy files:
  .\publish-to-iis.ps1 -App $App
Then re-run:
  .\setup-iis.ps1 -App $App
"@
}

$requiredFiles = @(
    $AppDll,
    "web.config",
    "appsettings.json"
)
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $PublishPath $file
    if (-not (Test-Path $fullPath)) {
        throw @"
Required file not found: $fullPath

Publish folder is empty or incomplete. Run:
  .\publish-to-iis.ps1 -App $App
Then re-run:
  .\setup-iis.ps1 -App $App
"@
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
$existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
$bindings = if ($existingSite) { @(Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue) } else { @() }

if (-not $existingSite) {
    New-Website -Name $SiteName -PhysicalPath $PublishPath -Port $Port -HostHeader $HostName -ApplicationPool $PoolName | Out-Null
    Write-Ok "Website created with host header $HostName."
}
elseif ($bindings.Count -eq 0) {
    Write-WarnMsg "Site exists but has no bindings. Recreating site."
    Invoke-IisCommand { Stop-Website -Name $SiteName } "Stop before recreate" | Out-Null
    Remove-Website -Name $SiteName
    New-Website -Name $SiteName -PhysicalPath $PublishPath -Port $Port -HostHeader $HostName -ApplicationPool $PoolName | Out-Null
    Write-Ok "Website recreated with host header $HostName."
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PublishPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $PoolName
    Set-HostnameBinding -Name $SiteName -HttpPort $Port -HostHeader $HostName
    Write-Ok "Website updated."
}

Set-HostnameBinding -Name $SiteName -HttpPort $Port -HostHeader $HostName

$bindings = @(Get-WebBinding -Name $SiteName -ErrorAction SilentlyContinue)
if ($bindings.Count -eq 0) {
    throw "Website still has no bindings. Run: Remove-Website -Name $SiteName then re-run this script."
}

Disable-DefaultWebSitePort -HttpPort $Port
Invoke-IisCommand {
    Remove-WebBinding -Name $SiteName -Protocol "https" -BindingInformation "*:443:"
} "Could not remove HTTPS binding" | Out-Null

Start-WebsiteSafe -Name $SiteName -PhysicalPath $PublishPath -ApplicationPool $PoolName -HttpPort $Port -HostHeader $HostName
Restart-WebAppPool -Name $PoolName
Write-Ok "$SiteName started: http://${HostName}/"

Write-Step "Site status"
Get-Website -Name $SiteName | Format-Table Name, State, PhysicalPath -AutoSize
Get-WebBinding -Name $SiteName | Format-Table protocol, bindingInformation -AutoSize

Write-Step "Firewall rule (port $Port)"
$ruleName = "Hamgam IIS HTTP $Port"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    Write-Ok "Firewall rule created."
}
else {
    Write-Ok "Firewall rule already exists."
}

$ip = Get-PrimaryIPv4
if (-not $SkipHostsFile) {
    Write-Step "Updating local hosts file for $HostName"
    if (-not $ip) {
        Write-WarnMsg "Could not detect LAN IP. Skipping hosts update."
    }
    else {
        Set-HostsEntry -Name $HostName -IpAddress $ip
        Write-Ok "hosts: $ip -> $HostName"
    }
}

Write-Step "Access URLs"
Write-Host ""
Write-Host "IIS setup completed successfully for $App." -ForegroundColor Green
Write-Host "  URL:     http://$HostName/" -ForegroundColor Green
if ($ip) {
    Write-Host "  Server:  $ip" -ForegroundColor Green
}
Write-Host ""
Write-Host "Network clients MUST resolve $HostName to this server IP." -ForegroundColor Yellow
Write-Host "  Option A (simple): on each PC run scripts\set-network-hosts.ps1 -ServerIp $ip" -ForegroundColor Yellow
Write-Host "  Option B (best):   add DNS A records on your router/DNS server" -ForegroundColor Yellow
Write-Host ""
Write-Host "Reminders:" -ForegroundColor Yellow
Write-Host "  1. SQL Server and database $DatabaseName must be ready (plus shared HamgamReference)."
Write-Host "  2. Check appsettings.json connection strings on this machine."
Write-Host "  3. Grant SQL access to: $poolIdentity"
Write-Host "  4. First login: $LoginHint"
Write-Host "  5. On error check: $PublishPath\logs\stdout_*.log"
Write-Host "  6. After reboot 500.30: run scripts\fix-iis-reboot-startup.ps1 -PoolName $PoolName"
Write-Host ""

$fixScript = Join-Path $PSScriptRoot "fix-iis-reboot-startup.ps1"
if (Test-Path $fixScript) {
    Write-Step "Applying reboot startup fix for pool $PoolName"
    & $fixScript -PoolName $PoolName
}
