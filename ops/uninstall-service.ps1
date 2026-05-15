# Stops + removes the Zenatur.LegacyBridge Windows Service.
# Requires elevation.
# Usage: powershell -ExecutionPolicy Bypass -File ops\uninstall-service.ps1

param([string]$ServiceName = "ZenaturLegacyBridge")

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
            ).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Must run elevated (Administrator)."
    exit 1
}

sc.exe query $ServiceName 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Service $ServiceName not installed. Nothing to do."
    exit 0
}

Write-Host "Stopping $ServiceName ..."
sc.exe stop $ServiceName | Out-Null
Start-Sleep -Seconds 3

Write-Host "Deleting $ServiceName ..."
sc.exe delete $ServiceName
if ($LASTEXITCODE -ne 0) { Write-Error "sc.exe delete failed."; exit 1 }

Write-Host "Done. Service $ServiceName removed."
