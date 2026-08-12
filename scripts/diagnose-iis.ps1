# Diagnose HTTP 500.30 for Cement / Transport on IIS
# Run as Administrator on the destination machine
#
# Examples:
#   .\diagnose-iis.ps1 -App Cement
#   .\diagnose-iis.ps1 -App Transport

param(
    [ValidateSet("Cement", "Transport")]
    [string]$App = "Cement",

    [string]$PublishPath,
    [string]$PoolName,
    [string]$AppDll,
    [string]$DatabaseName
)

$ErrorActionPreference = "Continue"

$defaults = @{
    Cement = @{
        PublishPath  = "C:\inetpub\Cement"
        PoolName     = "Cement"
        AppDll       = "HamgamCementWeb.Server.dll"
        DatabaseName = "HamgamNimroz"
    }
    Transport = @{
        PublishPath  = "C:\inetpub\Transport"
        PoolName     = "Transport"
        AppDll       = "HamgamTransport.Server.dll"
        DatabaseName = "HamgamTransport"
    }
}

$cfg = $defaults[$App]
if (-not $PublishPath)  { $PublishPath = $cfg.PublishPath }
if (-not $PoolName)     { $PoolName = $cfg.PoolName }
if (-not $AppDll)       { $AppDll = $cfg.AppDll }
if (-not $DatabaseName) { $DatabaseName = $cfg.DatabaseName }

$runtimeConfig = [IO.Path]::ChangeExtension($AppDll, ".runtimeconfig.json")

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string]$Message) {
    Write-Host "    OK: $Message" -ForegroundColor Green
}

function Write-Bad([string]$Message) {
    Write-Host "    FAIL: $Message" -ForegroundColor Red
}

function Write-WarnMsg([string]$Message) {
    Write-Host "    WARN: $Message" -ForegroundColor Yellow
}

Write-Step "App: $App"
Write-Ok "Path=$PublishPath | Pool=$PoolName | Dll=$AppDll"

Write-Step "Publish folder"
if (-not (Test-Path $PublishPath)) {
    Write-Bad "Folder not found: $PublishPath"
    Write-WarnMsg "Run: .\publish-to-iis.ps1 -App $App"
    exit 1
}
Write-Ok "Folder exists: $PublishPath"

Write-Step "Required files"
$required = @($AppDll, "web.config", "appsettings.json", $runtimeConfig)
foreach ($file in $required) {
    $path = Join-Path $PublishPath $file
    if (Test-Path $path) { Write-Ok $file } else { Write-Bad "Missing: $file" }
}

Write-Step ".NET runtime"
try {
    $dotnetInfo = & dotnet --list-runtimes 2>&1
    $aspnet = $dotnetInfo | Where-Object { $_ -match "Microsoft.AspNetCore.App 9\." }
    if ($aspnet) {
        Write-Ok "ASP.NET Core 9 runtime found"
        $aspnet | ForEach-Object { Write-Host "        $_" }
    } else {
        Write-Bad "ASP.NET Core 9 runtime NOT found"
        Write-WarnMsg "Install Hosting Bundle 9.0 then run: iisreset"
    }
} catch {
    Write-Bad "dotnet command not found in PATH"
}

Write-Step "IIS Hosting Bundle module"
$module = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (Test-Path $module) { Write-Ok "AspNetCoreModuleV2 installed" }
else { Write-Bad "AspNetCoreModuleV2 missing" }

Write-Step "stdout logs (most important for 500.30)"
$logsPath = Join-Path $PublishPath "logs"
if (-not (Test-Path $logsPath)) {
    Write-Bad "logs folder missing: $logsPath"
    Write-WarnMsg "Create it and grant Modify to IIS AppPool\$PoolName"
} else {
    $logFiles = Get-ChildItem $logsPath -Filter "stdout_*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending
    if (-not $logFiles) {
        Write-WarnMsg "No stdout_*.log yet. Browse the site once, then run this script again."
    } else {
        $latest = $logFiles | Select-Object -First 1
        Write-Ok "Latest log: $($latest.FullName)"
        Write-Host "-------- log content --------" -ForegroundColor DarkGray
        Get-Content $latest.FullName -Tail 80
        Write-Host "-----------------------------" -ForegroundColor DarkGray
    }
}

Write-Step "SQL Server connection (from appsettings.json)"
$appSettingsPath = Join-Path $PublishPath "appsettings.json"
if (Test-Path $appSettingsPath) {
    try {
        $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $conn = $json.ConnectionStrings.Local
        if ([string]::IsNullOrWhiteSpace($conn)) {
            Write-Bad "ConnectionStrings:Local is empty"
        } else {
            Write-Ok "Connection string found"
            Write-Host "        $conn" -ForegroundColor DarkGray

            Add-Type -AssemblyName "System.Data" -ErrorAction SilentlyContinue
            $masterConn = $conn -replace "Database=[^;]+", "Database=master"
            try {
                $sql = New-Object System.Data.SqlClient.SqlConnection $masterConn
                $sql.Open()
                $sql.Close()
                Write-Ok "SQL Server is reachable"
            } catch {
                Write-Bad "Cannot connect to SQL Server: $($_.Exception.Message)"
            }

            try {
                $sql = New-Object System.Data.SqlClient.SqlConnection $conn
                $sql.Open()
                $cmd = $sql.CreateCommand()
                $cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
                $tableCount = [int]$cmd.ExecuteScalar()
                $sql.Close()
                if ($tableCount -eq 0) {
                    Write-Bad "Database exists but has no tables. Run EF migrations first."
                } else {
                    Write-Ok "Database has $tableCount tables"
                }
            } catch {
                if ($_.Exception.Message -match "Cannot open database") {
                    Write-Bad "Database $DatabaseName does not exist. Create it and run migrations."
                } elseif ($_.Exception.Message -match "Login failed") {
                    Write-Bad "SQL login failed for IIS identity. Grant access to IIS AppPool\$PoolName"
                } else {
                    Write-Bad "Database error: $($_.Exception.Message)"
                }
            }
        }
    } catch {
        Write-Bad "Could not read appsettings.json: $($_.Exception.Message)"
    }
}

Write-Step "Run app manually (shows startup exception)"
Write-WarnMsg "Starting app for 8 seconds on port 5099..."
$env:ASPNETCORE_ENVIRONMENT = "Production"
$dll = Join-Path $PublishPath $AppDll
if (Test-Path $dll) {
    Push-Location $PublishPath
    $job = Start-Job -ScriptBlock {
        param($path, $dllName)
        Set-Location $path
        $env:ASPNETCORE_ENVIRONMENT = "Production"
        & dotnet $dllName --urls "http://127.0.0.1:5099" 2>&1
    } -ArgumentList $PublishPath, $AppDll

    Start-Sleep -Seconds 8
    $output = Receive-Job $job
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -Force -ErrorAction SilentlyContinue
    Pop-Location

    if ($output) {
        Write-Host "-------- manual run output --------" -ForegroundColor DarkGray
        $output | Select-Object -Last 40 | ForEach-Object { Write-Host $_ }
        Write-Host "-----------------------------------" -ForegroundColor DarkGray
    } else {
        Write-Ok "App seems to start without immediate error (check http://127.0.0.1:5099)"
    }
}
else {
    Write-Bad "DLL not found: $dll"
}

Write-Host ""
Write-Host "Common fixes for 500.30:" -ForegroundColor Yellow
Write-Host "  1. Install/repair ASP.NET Core 9 Hosting Bundle, then: iisreset"
Write-Host "  2. Create database $DatabaseName and run migrations"
Write-Host "  3. Grant SQL access to IIS AppPool\$PoolName (scripts\setup-sql-login.sql)"
Write-Host "  4. Fix ConnectionStrings:Local in appsettings.json"
Write-Host "  5. Ensure logs folder exists and is writable"
Write-Host "  6. If files missing: .\publish-to-iis.ps1 -App $App"
Write-Host ""
