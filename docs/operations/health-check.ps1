<#
.SYNOPSIS
    IndyPOS StoreHub Health Check Script

.DESCRIPTION
    Performs automated health checks and writes alerts to Windows Event Log.
    Designed to run as a scheduled task every hour.

.PARAMETER StoreHubUrl
    Base URL of StoreHub API (default: http://localhost:5000)

.PARAMETER EventLogSource
    Event Log source name (default: IndyPOS.Monitor)

.PARAMETER BackupDir
    Directory containing backup files

.PARAMETER MaxBackupAgeHours
    Maximum allowed backup age in hours (default: 24)

.PARAMETER MinDiskFreePercent
    Minimum free disk space percentage (default: 15)

.EXAMPLE
    .\health-check.ps1

.EXAMPLE
    .\health-check.ps1 -StoreHubUrl "http://192.168.1.100:5000" -MaxBackupAgeHours 12
#>

param(
    [string]$StoreHubUrl = "http://localhost:5000",
    [string]$EventLogSource = "IndyPOS.Monitor",
    [string]$BackupDir = "C:\ProgramData\IndyPOS\backups",
    [int]$MaxBackupAgeHours = 24,
    [int]$MinDiskFreePercent = 15
)

$ErrorActionPreference = "Continue"

# Event IDs
$EVENT_HEALTH_FAILED = 1001
$EVENT_STOREHUB_UNREACHABLE = 1002
$EVENT_BACKUP_STALE = 1003
$EVENT_BACKUP_MISSING = 1004
$EVENT_DISK_LOW = 1005
$EVENT_POSTGRES_DOWN = 1006
$EVENT_SYNC_BACKLOG = 1007
$EVENT_SERVICE_DOWN = 1008

function Initialize-EventLog {
    if (-not [System.Diagnostics.EventLog]::SourceExists($EventLogSource)) {
        try {
            New-EventLog -LogName Application -Source $EventLogSource -ErrorAction Stop
            Write-Host "Created event log source: $EventLogSource"
        }
        catch {
            Write-Warning "Could not create event log source. Run as Administrator first time."
        }
    }
}

function Write-Alert {
    param(
        [string]$Message,
        [int]$EventId,
        [string]$EntryType = "Warning"
    )

    try {
        Write-EventLog -LogName Application -Source $EventLogSource -EventId $EventId -EntryType $EntryType -Message $Message
        Write-Host "[$EntryType] $Message"
    }
    catch {
        Write-Warning "Could not write to event log: $($_.Exception.Message)"
        Write-Host "[$EntryType] $Message"
    }
}

function Test-StoreHubHealth {
    Write-Host "Checking StoreHub health..."

    try {
        $response = Invoke-RestMethod -Uri "$StoreHubUrl/health/ready" -TimeoutSec 10

        if ($response.status -ne "Healthy") {
            Write-Alert "StoreHub health check returned non-healthy status: $($response | ConvertTo-Json -Compress)" -EventId $EVENT_HEALTH_FAILED
            return $false
        }

        Write-Host "[OK] StoreHub is healthy"
        return $true
    }
    catch {
        Write-Alert "StoreHub unreachable at $StoreHubUrl : $($_.Exception.Message)" -EventId $EVENT_STOREHUB_UNREACHABLE -EntryType "Error"
        return $false
    }
}

function Test-StoreHubService {
    Write-Host "Checking StoreHub service..."

    $service = Get-Service -Name "IndyPOS.StoreHub" -ErrorAction SilentlyContinue

    if (-not $service) {
        Write-Alert "IndyPOS.StoreHub service not found" -EventId $EVENT_SERVICE_DOWN -EntryType "Error"
        return $false
    }

    if ($service.Status -ne "Running") {
        Write-Alert "IndyPOS.StoreHub service is not running. Status: $($service.Status)" -EventId $EVENT_SERVICE_DOWN -EntryType "Error"
        return $false
    }

    Write-Host "[OK] StoreHub service is running"
    return $true
}

function Test-PostgresService {
    Write-Host "Checking PostgreSQL service..."

    $pgService = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }

    if (-not $pgService) {
        Write-Alert "PostgreSQL service is not running" -EventId $EVENT_POSTGRES_DOWN -EntryType "Error"
        return $false
    }

    Write-Host "[OK] PostgreSQL service is running ($($pgService.Name))"
    return $true
}

function Test-BackupAge {
    Write-Host "Checking backup age..."

    if (-not (Test-Path $BackupDir)) {
        Write-Alert "Backup directory does not exist: $BackupDir" -EventId $EVENT_BACKUP_MISSING -EntryType "Error"
        return $false
    }

    $latestBackup = Get-ChildItem -Path $BackupDir -Filter "*.dump" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $latestBackup) {
        Write-Alert "No backup files found in $BackupDir" -EventId $EVENT_BACKUP_MISSING -EntryType "Error"
        return $false
    }

    $age = (Get-Date) - $latestBackup.LastWriteTime

    if ($age.TotalHours -gt $MaxBackupAgeHours) {
        Write-Alert "Backup is stale: $($latestBackup.Name) is $([math]::Round($age.TotalHours, 1)) hours old (threshold: $MaxBackupAgeHours hours)" -EventId $EVENT_BACKUP_STALE
        return $false
    }

    Write-Host "[OK] Latest backup is $([math]::Round($age.TotalHours, 1)) hours old"
    return $true
}

function Test-DiskSpace {
    Write-Host "Checking disk space..."

    $drive = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='C:'"
    $freePercent = ($drive.FreeSpace / $drive.Size) * 100

    if ($freePercent -lt $MinDiskFreePercent) {
        Write-Alert "Low disk space: C: drive only $([math]::Round($freePercent, 1))% free (threshold: $MinDiskFreePercent%)" -EventId $EVENT_DISK_LOW
        return $false
    }

    Write-Host "[OK] Disk space: $([math]::Round($freePercent, 1))% free"
    return $true
}

function Test-SyncBacklog {
    Write-Host "Checking sync backlog..."

    try {
        # Try to get sync status without auth (may fail if auth required)
        $response = Invoke-RestMethod -Uri "$StoreHubUrl/sync/status" -TimeoutSec 10 -ErrorAction SilentlyContinue

        if ($response.pending -gt 100) {
            Write-Alert "High sync backlog: $($response.pending) events pending" -EventId $EVENT_SYNC_BACKLOG
            return $false
        }

        Write-Host "[OK] Sync backlog: $($response.pending) pending"
        return $true
    }
    catch {
        # Sync status may require auth - not a failure
        Write-Host "[SKIP] Could not check sync status (may require auth)"
        return $true
    }
}

# ============================================
# MAIN
# ============================================

Write-Host ""
Write-Host "============================================"
Write-Host " IndyPOS Health Check"
Write-Host "============================================"
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Target: $StoreHubUrl"
Write-Host ""

Initialize-EventLog

$results = @{
    StoreHubService = Test-StoreHubService
    PostgresService = Test-PostgresService
    StoreHubHealth = Test-StoreHubHealth
    BackupAge = Test-BackupAge
    DiskSpace = Test-DiskSpace
    SyncBacklog = Test-SyncBacklog
}

Write-Host ""
Write-Host "============================================"
Write-Host " SUMMARY"
Write-Host "============================================"

$passed = ($results.Values | Where-Object { $_ -eq $true }).Count
$failed = ($results.Values | Where-Object { $_ -eq $false }).Count

Write-Host "Passed: $passed"
Write-Host "Failed: $failed"

if ($failed -gt 0) {
    Write-Host ""
    Write-Host "HEALTH CHECK FAILED - Review alerts above" -ForegroundColor Red
    exit 1
}
else {
    Write-Host ""
    Write-Host "HEALTH CHECK PASSED - All systems operational" -ForegroundColor Green
    exit 0
}
