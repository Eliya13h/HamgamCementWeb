# استارت هم‌زمان سیمان + ترانسپورت (بک‌اند + فرانت با SpaProxy)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Write-Host "Starting Hamgam Cement (7294 / 61829)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; dotnet run --project 'HamgamCementWeb.Server\HamgamCementWeb.Server.csproj' --launch-profile https"
)

Start-Sleep -Seconds 2

Write-Host "Starting Hamgam Transport (7295 / 61830)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; dotnet run --project 'HamgamTransport.Server\HamgamTransport.Server.csproj' --launch-profile https"
)

Start-Sleep -Seconds 8

Write-Host "Opening browsers..." -ForegroundColor Cyan
Start-Process "https://localhost:7294"
Start-Process "https://localhost:7295"

Write-Host ""
Write-Host "Both systems are starting in separate windows." -ForegroundColor Green
Write-Host "  Cement:    https://localhost:7294" -ForegroundColor Yellow
Write-Host "  Transport: https://localhost:7295" -ForegroundColor Yellow
