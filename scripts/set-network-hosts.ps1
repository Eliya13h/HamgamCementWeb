# Adds Cement.local / Transport.local to the Windows hosts file.
# Run PowerShell as Administrator on EACH client PC (and on the server if needed).
#
# Examples:
#   .\set-network-hosts.ps1 -ServerIp 192.168.1.50
#   .\set-network-hosts.ps1 -ServerIp 192.168.1.50 -CementHost Cement.local -TransportHost Transport.local

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerIp,

    [string]$CementHost = "Cement.local",
    [string]$TransportHost = "Transport.local"
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    throw "Run this script as Administrator."
}

if ($ServerIp -notmatch '^\d{1,3}(\.\d{1,3}){3}$') {
    throw "Invalid IPv4 address: $ServerIp"
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

Set-HostsEntry -Name $CementHost -IpAddress $ServerIp
Set-HostsEntry -Name $TransportHost -IpAddress $ServerIp

Write-Host "hosts updated:" -ForegroundColor Green
Write-Host "  $ServerIp  $CementHost"
Write-Host "  $ServerIp  $TransportHost"
Write-Host ""
Write-Host "Test:" -ForegroundColor Cyan
Write-Host "  ping $CementHost"
Write-Host "  ping $TransportHost"
Write-Host "  http://$CementHost/"
Write-Host "  http://$TransportHost/"
