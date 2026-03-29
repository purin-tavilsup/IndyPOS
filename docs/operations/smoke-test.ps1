<#
.SYNOPSIS
    IndyPOS StoreHub Smoke Test Script

.DESCRIPTION
    Performs health and validation checks after deployment.
    Run this script to verify StoreHub is operating correctly.

.PARAMETER StoreHubUrl
    Base URL of StoreHub API (default: http://localhost:5000)

.PARAMETER Username
    Username for authentication (required for protected endpoints)

.PARAMETER Password
    Password for authentication

.PARAMETER Verbose
    Show detailed output

.EXAMPLE
    .\smoke-test.ps1 -Username "admin" -Password "password123"

.EXAMPLE
    .\smoke-test.ps1 -StoreHubUrl "http://192.168.1.100:5000" -Username "admin" -Password "password123"
#>

param(
    [string]$StoreHubUrl = "http://localhost:5000",
    [string]$Username = "",
    [string]$Password = "",
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"
$script:PassCount = 0
$script:FailCount = 0
$script:Token = $null

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = ""
    )

    if ($Passed) {
        Write-Host "[PASS] " -ForegroundColor Green -NoNewline
        $script:PassCount++
    } else {
        Write-Host "[FAIL] " -ForegroundColor Red -NoNewline
        $script:FailCount++
    }

    Write-Host $TestName

    if ($Details -and $VerboseOutput) {
        Write-Host "       $Details" -ForegroundColor Gray
    }
}

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = "application/json"
        }

        $response = Invoke-WebRequest @params
        $passed = $ExpectedStatus -contains $response.StatusCode

        Write-TestResult -TestName $Name -Passed $passed -Details "Status: $($response.StatusCode)"

        return @{
            Passed = $passed
            StatusCode = $response.StatusCode
            Content = $response.Content
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        $passed = $ExpectedStatus -contains $statusCode
        Write-TestResult -TestName $Name -Passed $passed -Details "Error: $($_.Exception.Message)"

        return @{
            Passed = $passed
            StatusCode = $statusCode
            Error = $_.Exception.Message
        }
    }
}

function Get-AuthToken {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        Write-Host "Skipping authenticated tests (no credentials provided)" -ForegroundColor Yellow
        return $null
    }

    try {
        $response = Invoke-RestMethod -Uri "$StoreHubUrl/auth/login" -Method POST -Body (@{
            username = $Username
            password = $Password
        } | ConvertTo-Json) -ContentType "application/json"

        if ($response.token) {
            Write-TestResult -TestName "Authentication" -Passed $true -Details "Token obtained"
            return $response.token
        }
    }
    catch {
        Write-TestResult -TestName "Authentication" -Passed $false -Details $_.Exception.Message
    }

    return $null
}

# ============================================
# SMOKE TESTS
# ============================================

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " IndyPOS StoreHub Smoke Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Target: $StoreHubUrl"
Write-Host "Time:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# ----- Health Checks -----
Write-Host "--- Health Checks ---" -ForegroundColor Yellow

Test-Endpoint -Name "Health endpoint" -Url "$StoreHubUrl/health"
Test-Endpoint -Name "Ready endpoint (includes DB)" -Url "$StoreHubUrl/health/ready"

# ----- PostgreSQL Connection -----
Write-Host ""
Write-Host "--- Database Checks ---" -ForegroundColor Yellow

$pgRunning = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
Write-TestResult -TestName "PostgreSQL service running" -Passed ($null -ne $pgRunning) -Details ($pgRunning | Select-Object -First 1).Name

# ----- Authentication -----
Write-Host ""
Write-Host "--- Authentication ---" -ForegroundColor Yellow

$script:Token = Get-AuthToken

# ----- API Endpoints (Authenticated) -----
if ($script:Token) {
    $authHeaders = @{ Authorization = "Bearer $($script:Token)" }

    Write-Host ""
    Write-Host "--- API Endpoints ---" -ForegroundColor Yellow

    # Products endpoint
    $productsResult = Test-Endpoint -Name "GET /products" -Url "$StoreHubUrl/products" -Headers $authHeaders

    if ($productsResult.Passed -and $productsResult.Content) {
        try {
            $products = $productsResult.Content | ConvertFrom-Json
            $productCount = if ($products -is [array]) { $products.Count } else { 1 }
            Write-TestResult -TestName "Products exist in database" -Passed ($productCount -gt 0) -Details "Count: $productCount"
        }
        catch {
            Write-TestResult -TestName "Products exist in database" -Passed $false -Details "Parse error"
        }
    }

    # Sync status endpoint
    Test-Endpoint -Name "GET /sync/status" -Url "$StoreHubUrl/sync/status" -Headers $authHeaders

    # Reports endpoints
    $today = (Get-Date).ToString("yyyy-MM-dd")
    Test-Endpoint -Name "GET /reports/sales-summary" -Url "$StoreHubUrl/reports/sales-summary?fromDate=$today&toDate=$today" -Headers $authHeaders
}

# ----- Backup Status -----
Write-Host ""
Write-Host "--- Backup Status ---" -ForegroundColor Yellow

$backupDir = "C:\ProgramData\IndyPOS\backups"
if (Test-Path $backupDir) {
    $latestBackup = Get-ChildItem -Path $backupDir -Filter "*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($latestBackup) {
        $backupAge = (Get-Date) - $latestBackup.LastWriteTime
        $backupFresh = $backupAge.TotalHours -lt 24
        Write-TestResult -TestName "Backup exists (< 24h old)" -Passed $backupFresh -Details "Last: $($latestBackup.Name) ($([math]::Round($backupAge.TotalHours, 1))h ago)"
    }
    else {
        Write-TestResult -TestName "Backup exists (< 24h old)" -Passed $false -Details "No .dump files found"
    }
}
else {
    Write-TestResult -TestName "Backup directory exists" -Passed $false -Details "Path: $backupDir"
}

# ----- Disk Space -----
Write-Host ""
Write-Host "--- System Health ---" -ForegroundColor Yellow

$drive = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='C:'"
$freePercent = [math]::Round(($drive.FreeSpace / $drive.Size) * 100, 1)
Write-TestResult -TestName "Disk space > 15% free" -Passed ($freePercent -gt 15) -Details "C: drive $freePercent% free"

# ----- Service Status -----
$storeHubService = Get-Service -Name "IndyPOS.StoreHub" -ErrorAction SilentlyContinue
if ($storeHubService) {
    Write-TestResult -TestName "StoreHub service running" -Passed ($storeHubService.Status -eq "Running") -Details "Status: $($storeHubService.Status)"
}
else {
    Write-TestResult -TestName "StoreHub service exists" -Passed $false -Details "Service not found"
}

# ============================================
# SUMMARY
# ============================================

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " SUMMARY" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Passed: $script:PassCount" -ForegroundColor Green
Write-Host "Failed: $script:FailCount" -ForegroundColor $(if ($script:FailCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($script:FailCount -gt 0) {
    Write-Host "SMOKE TEST FAILED - Review failures above" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "SMOKE TEST PASSED - All checks successful" -ForegroundColor Green
    exit 0
}
