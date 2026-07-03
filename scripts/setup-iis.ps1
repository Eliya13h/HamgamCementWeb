# تنظیم IIS برای HamgamCementWeb (بعد از publish)
# اجرا با PowerShell Administrator
$ErrorActionPreference = "Stop"
$siteName = "HamgamCementWeb"
$poolName = "HamgamCementWeb"
$publishPath = "C:\inetpub\HamgamCementWeb"
$port = 80

Import-Module WebAdministration

if (-not (Test-Path "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")) {
    Write-Host "ASP.NET Core Hosting Bundle نصب نیست." -ForegroundColor Red
    Write-Host "دانلود و نصب: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    Write-Host "گزینه: Hosting Bundle (نه فقط SDK)" -ForegroundColor Yellow
    Write-Host "بعد از نصب: iisreset" -ForegroundColor Yellow
}

if (-not (Test-Path $publishPath)) {
    throw "Publish folder not found: $publishPath"
}

if (-not (Test-Path "IIS:\AppPools\$poolName")) {
    New-WebAppPool -Name $poolName
}
Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"

New-Item -ItemType Directory -Force -Path "$publishPath\logs" | Out-Null
icacls "$publishPath\logs" /grant "IIS AppPool\${poolName}:(OI)(CI)M" /T | Out-Null
icacls $publishPath /grant "IIS AppPool\${poolName}:(OI)(CI)RX" /T | Out-Null

if (-not (Test-Path "IIS:\Sites\$siteName")) {
    New-Website -Name $siteName -PhysicalPath $publishPath -Port $port -ApplicationPool $poolName
} else {
    Set-ItemProperty "IIS:\Sites\$siteName" -Name physicalPath -Value $publishPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name applicationPool -Value $poolName
}

Stop-Website -Name "Default Web Site" -ErrorAction SilentlyContinue
Remove-WebBinding -Name $siteName -Protocol "https" -BindingInformation "*:443:" -ErrorAction SilentlyContinue

Restart-WebAppPool -Name $poolName
Write-Host "IIS site '$siteName' ready at http://localhost:$port" -ForegroundColor Green
