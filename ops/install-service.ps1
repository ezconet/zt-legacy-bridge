# Installs Zenatur.LegacyBridge as a Windows Service.
# Requires elevation (sc.exe create/config need admin).
# Usage:  powershell -ExecutionPolicy Bypass -File ops\install-service.ps1
#         powershell -ExecutionPolicy Bypass -File ops\install-service.ps1 -SkipPublish

param(
    [string]$ServiceName = "ZenaturLegacyBridge",
    [string]$DisplayName = "Zenatur Legacy Bridge",
    [string]$PublishDir  = "$PSScriptRoot\..\publish",
    [string]$Project     = "$PSScriptRoot\..\src\Zenatur.LegacyBridge\Zenatur.LegacyBridge.csproj",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
            ).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Must run elevated (Administrator). sc.exe create/config/failure require admin."
    exit 1
}

if (-not $SkipPublish) {
    Write-Host "Publishing $Project -> $PublishDir ..."
    dotnet publish $Project -c Release -o $PublishDir --nologo
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed."; exit 1 }
}

$exePath = Join-Path (Resolve-Path $PublishDir) "Zenatur.LegacyBridge.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Executable not found: $exePath"
    exit 1
}

$existing = sc.exe query $ServiceName 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Service $ServiceName already exists. Stopping + deleting before recreate ..."
    sc.exe stop $ServiceName | Out-Null
    Start-Sleep -Seconds 3
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating service $ServiceName -> $exePath"
# binPath quoted; spaces after = are required by sc.exe syntax.
sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto DisplayName= "$DisplayName"
if ($LASTEXITCODE -ne 0) { Write-Error "sc.exe create failed."; exit 1 }

sc.exe description $ServiceName "Anti-Corruption Layer entre CIOT API/TMS e SQL Server legado Zenatur." | Out-Null

# Restart on failure: 1st/2nd/3rd failure -> restart after 60s. Reset count after 24h.
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "Starting $ServiceName ..."
sc.exe start $ServiceName
if ($LASTEXITCODE -ne 0) { Write-Error "sc.exe start failed (check Event Log / logs/bridge-*.log)."; exit 1 }

Write-Host "Done. Service $ServiceName installed + started (auto-start, restart-on-failure)."
Write-Host "Health: curl http://localhost:5188/health/live"
