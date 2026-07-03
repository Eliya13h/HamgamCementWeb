# اجرای HamgamCementWeb روی شبکه محلی (HTTP پورت 5085)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$client = Join-Path $root "hamgamcementweb.client"
$server = Join-Path $root "HamgamCementWeb.Server"
$wwwroot = Join-Path $server "wwwroot"
$dist = Join-Path $client "dist"
$port = 5085

Write-Host "==> npm install & build (client)" -ForegroundColor Cyan
Push-Location $client
npm install
npm run build
Pop-Location

if (-not (Test-Path (Join-Path $dist "index.html"))) {
    throw "Build failed: dist\index.html not found."
}

Write-Host "==> copy dist -> wwwroot" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
robocopy $dist $wwwroot /MIR /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

Write-Host "==> firewall rule (port $port)" -ForegroundColor Cyan
$ruleName = "HamgamCementWeb"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    try {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow | Out-Null
        Write-Host "Firewall rule created." -ForegroundColor Green
    } catch {
        Write-Warning "Could not create firewall rule (run as Administrator). Others may not connect: $_"
    }
} else {
    Write-Host "Firewall rule already exists." -ForegroundColor Green
}

$ip = (Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.PrefixOrigin -ne "WellKnown" } |
    Select-Object -First 1 -ExpandProperty IPAddress)

Write-Host ""
Write-Host "Local:   http://localhost:$port" -ForegroundColor Green
if ($ip) { Write-Host "Network: http://${ip}:$port" -ForegroundColor Green }
Write-Host ""

Push-Location $server
dotnet run --launch-profile network
Pop-Location
