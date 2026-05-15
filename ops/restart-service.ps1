# Restarts the Zenatur.LegacyBridge Windows Service.
# Requires elevation.
# Usage: powershell -ExecutionPolicy Bypass -File ops\restart-service.ps1

param([string]$ServiceName = "ZenaturLegacyBridge")

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
            ).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Must run elevated (Administrator)."
    exit 1
}

Write-Host "Stopping $ServiceName ..."
sc.exe stop $ServiceName | Out-Null
Start-Sleep -Seconds 3

Write-Host "Starting $ServiceName ..."
sc.exe start $ServiceName
if ($LASTEXITCODE -ne 0) { Write-Error "sc.exe start failed."; exit 1 }

Write-Host "Done. $ServiceName restarted."
