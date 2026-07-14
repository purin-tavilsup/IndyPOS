<#
.SYNOPSIS
    End-to-end smoke test for IndyPOS StoreHub.

.DESCRIPTION
    Comprehensive E2E test suite for IndyPOS StoreHub API.

    TEST CATEGORIES:
    1. Health - live, ready, version endpoints
    2. Authentication - login (valid/invalid), get current user
    3. Products - CRUD operations, search, barcode lookup
    4. Sales - complete sale, verify inventory deduction
    5. Pay Later - create pay later sale, record payment
    6. Reports - sales summary, invoices, product sales
    7. Cleanup - delete test data

    WHAT GETS TESTED:
    - Create product -> Update product -> Adjust inventory
    - Complete cash sale -> Verify inventory deducted -> Verify in reports
    - Complete pay later sale -> List accounts -> Record payment
    - All report endpoints

    PREREQUISITES:
    - StoreHub must be running (via Aspire or standalone)
    - Database must be seeded with test data

    SEEDED TEST USERS (Development):
    - admin / admin123     (SystemAdmin)
    - manager / manager123 (StoreManager)
    - cashier / cashier123 (Cashier)

.PARAMETER BaseUrl
    StoreHub API base URL. Default: http://localhost:5012

.PARAMETER Verbose
    Show detailed output for each test.

.EXAMPLE
    # Test against Aspire (check dashboard for actual port)
    .\smoke-test.ps1 -BaseUrl "http://localhost:5012"

.EXAMPLE
    # Test against production deployment
    .\smoke-test.ps1 -BaseUrl "http://192.168.1.100:5000"
#>

param(
    [string]$BaseUrl = "http://localhost:5012",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$script:PassCount = 0
$script:FailCount = 0
$script:Token = $null

function Write-TestHeader {
    param([string]$Title)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = ""
    )

    if ($Passed) {
        Write-Host "[PASS] $TestName" -ForegroundColor Green
        $script:PassCount++
    } else {
        Write-Host "[FAIL] $TestName" -ForegroundColor Red
        $script:FailCount++
    }

    if ($Verbose -and $Details) {
        Write-Host "       $Details" -ForegroundColor Gray
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Method = "GET",
        [string]$Endpoint,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [bool]$IgnoreError = $false
    )

    $uri = "$BaseUrl$Endpoint"

    try {
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response; StatusCode = 200 }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($IgnoreError) {
            return @{ Success = $false; Error = $_.Exception.Message; StatusCode = $statusCode }
        }
        throw
    }
}

function Get-AuthHeaders {
    if ($script:Token) {
        return @{ "Authorization" = "Bearer $($script:Token)" }
    }
    return @{}
}

# ============================================
# TEST SUITE
# ============================================

Write-Host ""
Write-Host "IndyPOS StoreHub Smoke Test" -ForegroundColor Magenta
Write-Host "Base URL: $BaseUrl" -ForegroundColor Magenta
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Magenta

# --------------------------------------------
# 1. Health Checks
# --------------------------------------------
Write-TestHeader "1. Health Checks"

# Test: Health Live
try {
    $result = Invoke-ApiRequest -Endpoint "/health/live" -IgnoreError $true
    Write-TestResult -TestName "GET /health/live" -Passed $result.Success -Details "Service is alive"
}
catch {
    Write-TestResult -TestName "GET /health/live" -Passed $false -Details $_.Exception.Message
}

# Test: Health Ready
# Standard ASP.NET Core HealthChecks responds with plain-text "Healthy" + 200
# when all "ready"-tagged checks pass (including DbContextCheck). The HTTP
# status code IS the contract — don't depend on body shape.
try {
    $result = Invoke-ApiRequest -Endpoint "/health/ready" -IgnoreError $true
    Write-TestResult -TestName "GET /health/ready" -Passed $result.Success -Details "Status: $($result.StatusCode)"
}
catch {
    Write-TestResult -TestName "GET /health/ready" -Passed $false -Details $_.Exception.Message
}

# Test: Version
try {
    $result = Invoke-ApiRequest -Endpoint "/version" -IgnoreError $true
    $hasVersion = $result.Success -and $result.Data.version
    Write-TestResult -TestName "GET /version" -Passed $hasVersion -Details "Version: $($result.Data.version)"
}
catch {
    Write-TestResult -TestName "GET /version" -Passed $false -Details $_.Exception.Message
}

# --------------------------------------------
# 2. Authentication
# --------------------------------------------
Write-TestHeader "2. Authentication"

# Test: Login with invalid credentials
try {
    $result = Invoke-ApiRequest -Method "POST" -Endpoint "/auth/login" -Body @{
        username = "invalid_user"
        password = "wrong_password"
    } -IgnoreError $true
    $rejected = -not $result.Success -and $result.StatusCode -eq 401
    Write-TestResult -TestName "POST /auth/login (invalid)" -Passed $rejected -Details "Correctly rejected invalid credentials"
}
catch {
    Write-TestResult -TestName "POST /auth/login (invalid)" -Passed $false -Details $_.Exception.Message
}

# Test: Login as admin (default seeded user)
try {
    $result = Invoke-ApiRequest -Method "POST" -Endpoint "/auth/login" -Body @{
        username = "admin"
        password = "admin123"
    } -IgnoreError $true

    if ($result.Success -and $result.Data.token) {
        $script:Token = $result.Data.token
        Write-TestResult -TestName "POST /auth/login (admin)" -Passed $true -Details "Token received"
    } else {
        Write-TestResult -TestName "POST /auth/login (admin)" -Passed $false -Details "No token in response"
    }
}
catch {
    Write-TestResult -TestName "POST /auth/login (admin)" -Passed $false -Details $_.Exception.Message
}

# Test: Get current user
if ($script:Token) {
    try {
        $result = Invoke-ApiRequest -Endpoint "/auth/me" -Headers (Get-AuthHeaders) -IgnoreError $true
        $hasUser = $result.Success -and $result.Data.username
        Write-TestResult -TestName "GET /auth/me" -Passed $hasUser -Details "User: $($result.Data.username)"
    }
    catch {
        Write-TestResult -TestName "GET /auth/me" -Passed $false -Details $_.Exception.Message
    }
}

# --------------------------------------------
# 3. Products
# --------------------------------------------
Write-TestHeader "3. Products"

if ($script:Token) {
    # Test: Get all products
    try {
        $result = Invoke-ApiRequest -Endpoint "/products" -Headers (Get-AuthHeaders) -IgnoreError $true
        $hasProducts = $result.Success -and $result.Data -is [array]
        $count = if ($hasProducts) { $result.Data.Count } else { 0 }
        Write-TestResult -TestName "GET /products" -Passed $hasProducts -Details "Found $count products"
    }
    catch {
        Write-TestResult -TestName "GET /products" -Passed $false -Details $_.Exception.Message
    }

    # Test: Search products
    try {
        $result = Invoke-ApiRequest -Endpoint "/products?search=test" -Headers (Get-AuthHeaders) -IgnoreError $true
        $isArray = $result.Success -and $result.Data -is [array]
        Write-TestResult -TestName "GET /products?search=test" -Passed $isArray -Details "Search returned array"
    }
    catch {
        Write-TestResult -TestName "GET /products?search=test" -Passed $false -Details $_.Exception.Message
    }

    # Test: Get product by barcode (may not exist)
    try {
        $result = Invoke-ApiRequest -Endpoint "/products/barcode/1234567890" -Headers (Get-AuthHeaders) -IgnoreError $true
        # Either found (200) or not found (404) is acceptable
        $acceptable = $result.Success -or $result.StatusCode -eq 404
        Write-TestResult -TestName "GET /products/barcode/{code}" -Passed $acceptable -Details "Barcode lookup works"
    }
    catch {
        Write-TestResult -TestName "GET /products/barcode/{code}" -Passed $false -Details $_.Exception.Message
    }
    # Test: Create product
    $testBarcode = "TEST" + (Get-Date -Format "yyyyMMddHHmmss")
    $script:CreatedProductId = $null
    try {
        $result = Invoke-ApiRequest -Method "POST" -Endpoint "/products" -Headers (Get-AuthHeaders) -Body @{
            barcode = $testBarcode
            description = "E2E Test Product"
            unitPrice = 99.99
            category = "Test"
            quantityInStock = 100
            isActive = $true
        } -IgnoreError $true

        if ($result.Success -and $result.Data.id) {
            $script:CreatedProductId = $result.Data.id
            Write-TestResult -TestName "POST /products (create)" -Passed $true -Details "Created: $testBarcode"
        } else {
            Write-TestResult -TestName "POST /products (create)" -Passed $false -Details "Failed to create product"
        }
    }
    catch {
        Write-TestResult -TestName "POST /products (create)" -Passed $false -Details $_.Exception.Message
    }

    # Test: Update product
    if ($script:CreatedProductId) {
        try {
            $result = Invoke-ApiRequest -Method "PUT" -Endpoint "/products/$($script:CreatedProductId)" -Headers (Get-AuthHeaders) -Body @{
                barcode = $testBarcode
                description = "E2E Test Product (Updated)"
                unitPrice = 149.99
                category = "Test"
                quantityInStock = 100
                isActive = $true
            } -IgnoreError $true

            $updated = $result.Success -and $result.Data.unitPrice -eq 149.99
            Write-TestResult -TestName "PUT /products/{id} (update)" -Passed $updated -Details "Price updated to 149.99"
        }
        catch {
            Write-TestResult -TestName "PUT /products/{id} (update)" -Passed $false -Details $_.Exception.Message
        }

        # Test: Adjust inventory
        try {
            $result = Invoke-ApiRequest -Method "POST" -Endpoint "/inventory/adjust" -Headers (Get-AuthHeaders) -Body @{
                productId = $script:CreatedProductId
                adjustment = -10
                reason = "E2E Test adjustment"
            } -IgnoreError $true

            Write-TestResult -TestName "POST /inventory/adjust" -Passed $result.Success -Details "Adjusted by -10"
        }
        catch {
            Write-TestResult -TestName "POST /inventory/adjust" -Passed $false -Details $_.Exception.Message
        }
    }
} else {
    Write-Host "[SKIP] Products tests - no auth token" -ForegroundColor Yellow
}

# --------------------------------------------
# 4. Sales (Complete Sale Flow)
# --------------------------------------------
Write-TestHeader "4. Sales"

$script:CreatedInvoiceId = $null
if ($script:Token -and $script:CreatedProductId) {
    # Test: Complete a sale
    try {
        $result = Invoke-ApiRequest -Method "POST" -Endpoint "/sales/complete" -Headers (Get-AuthHeaders) -Body @{
            items = @(
                @{
                    productId = $script:CreatedProductId
                    quantity = 2
                    unitPrice = 149.99
                }
            )
            payments = @(
                @{
                    paymentType = "Cash"
                    amount = 299.98
                }
            )
        } -IgnoreError $true

        if ($result.Success -and $result.Data.invoiceId) {
            $script:CreatedInvoiceId = $result.Data.invoiceId
            Write-TestResult -TestName "POST /sales/complete" -Passed $true -Details "Invoice: $($result.Data.invoiceId)"
        } else {
            Write-TestResult -TestName "POST /sales/complete" -Passed $false -Details "No invoice ID returned"
        }
    }
    catch {
        Write-TestResult -TestName "POST /sales/complete" -Passed $false -Details $_.Exception.Message
    }

    # Test: Verify inventory was deducted
    if ($script:CreatedProductId) {
        try {
            $result = Invoke-ApiRequest -Endpoint "/products" -Headers (Get-AuthHeaders) -IgnoreError $true
            $product = $result.Data | Where-Object { $_.id -eq $script:CreatedProductId }
            # Started with 100, adjusted -10, sold 2 = 88
            $expectedQty = 88
            $actualQty = $product.quantityInStock
            $correct = $actualQty -eq $expectedQty
            Write-TestResult -TestName "Verify inventory deducted" -Passed $correct -Details "Expected: $expectedQty, Actual: $actualQty"
        }
        catch {
            Write-TestResult -TestName "Verify inventory deducted" -Passed $false -Details $_.Exception.Message
        }
    }

    # Test: Verify invoice in reports
    if ($script:CreatedInvoiceId) {
        try {
            $today = Get-Date -Format "yyyy-MM-dd"
            $result = Invoke-ApiRequest -Endpoint "/reports/invoices?date=$today" -Headers (Get-AuthHeaders) -IgnoreError $true
            $found = $result.Data | Where-Object { $_.id -eq $script:CreatedInvoiceId }
            Write-TestResult -TestName "Verify invoice in reports" -Passed ($null -ne $found) -Details "Invoice found in today's report"
        }
        catch {
            Write-TestResult -TestName "Verify invoice in reports" -Passed $false -Details $_.Exception.Message
        }
    }
} else {
    Write-Host "[SKIP] Sales tests - no auth token or product" -ForegroundColor Yellow
}

# --------------------------------------------
# 5. Pay Later
# --------------------------------------------
Write-TestHeader "5. Pay Later"

$script:PayLaterId = $null
if ($script:Token -and $script:CreatedProductId) {
    # Test: Create pay later sale
    try {
        $result = Invoke-ApiRequest -Method "POST" -Endpoint "/sales/complete" -Headers (Get-AuthHeaders) -Body @{
            items = @(
                @{
                    productId = $script:CreatedProductId
                    quantity = 1
                    unitPrice = 149.99
                }
            )
            payments = @(
                @{
                    paymentType = "PayLater"
                    amount = 149.99
                    payLaterAccountName = "E2E Test Customer"
                }
            )
        } -IgnoreError $true

        if ($result.Success) {
            Write-TestResult -TestName "POST /sales/complete (pay later)" -Passed $true -Details "Pay later sale created"
        } else {
            Write-TestResult -TestName "POST /sales/complete (pay later)" -Passed $false -Details "Failed to create"
        }
    }
    catch {
        Write-TestResult -TestName "POST /sales/complete (pay later)" -Passed $false -Details $_.Exception.Message
    }

    # Test: List pay later accounts
    try {
        $result = Invoke-ApiRequest -Endpoint "/pay-later" -Headers (Get-AuthHeaders) -IgnoreError $true
        $isArray = $result.Success -and $result.Data -is [array]
        $count = if ($isArray) { $result.Data.Count } else { 0 }

        # Find our test account
        $testAccount = $result.Data | Where-Object { $_.accountName -eq "E2E Test Customer" } | Select-Object -First 1
        if ($testAccount) {
            $script:PayLaterId = $testAccount.id
        }

        Write-TestResult -TestName "GET /pay-later" -Passed $isArray -Details "Found $count accounts"
    }
    catch {
        Write-TestResult -TestName "GET /pay-later" -Passed $false -Details $_.Exception.Message
    }

    # Test: Record payment
    if ($script:PayLaterId) {
        try {
            $result = Invoke-ApiRequest -Method "POST" -Endpoint "/pay-later/$($script:PayLaterId)/payments" -Headers (Get-AuthHeaders) -Body @{
                amount = 50.00
                note = "E2E Test partial payment"
            } -IgnoreError $true

            Write-TestResult -TestName "POST /pay-later/{id}/payments" -Passed $result.Success -Details "Recorded $50 payment"
        }
        catch {
            Write-TestResult -TestName "POST /pay-later/{id}/payments" -Passed $false -Details $_.Exception.Message
        }
    }
} else {
    Write-Host "[SKIP] Pay Later tests - no auth token or product" -ForegroundColor Yellow
}

# --------------------------------------------
# 6. Reports
# --------------------------------------------
Write-TestHeader "6. Reports"

if ($script:Token) {
    # Test: Get sales summary
    try {
        $today = Get-Date -Format "yyyy-MM-dd"
        $result = Invoke-ApiRequest -Endpoint "/reports/sales-summary?date=$today" -Headers (Get-AuthHeaders) -IgnoreError $true
        Write-TestResult -TestName "GET /reports/sales-summary" -Passed $result.Success -Details "Date: $today"
    }
    catch {
        Write-TestResult -TestName "GET /reports/sales-summary" -Passed $false -Details $_.Exception.Message
    }

    # Test: Get invoices
    try {
        $today = Get-Date -Format "yyyy-MM-dd"
        $result = Invoke-ApiRequest -Endpoint "/reports/invoices?date=$today" -Headers (Get-AuthHeaders) -IgnoreError $true
        $isArray = $result.Success -and $result.Data -is [array]
        Write-TestResult -TestName "GET /reports/invoices" -Passed $isArray -Details "Invoices endpoint works"
    }
    catch {
        Write-TestResult -TestName "GET /reports/invoices" -Passed $false -Details $_.Exception.Message
    }

    # Test: Get product sales
    try {
        $today = Get-Date -Format "yyyy-MM-dd"
        $result = Invoke-ApiRequest -Endpoint "/reports/product-sales?startDate=$today&endDate=$today" -Headers (Get-AuthHeaders) -IgnoreError $true
        $isArray = $result.Success -and $result.Data -is [array]
        Write-TestResult -TestName "GET /reports/product-sales" -Passed $isArray -Details "Product sales endpoint works"
    }
    catch {
        Write-TestResult -TestName "GET /reports/product-sales" -Passed $false -Details $_.Exception.Message
    }

    # Test: Get pay later report
    try {
        $result = Invoke-ApiRequest -Endpoint "/reports/pay-later" -Headers (Get-AuthHeaders) -IgnoreError $true
        Write-TestResult -TestName "GET /reports/pay-later" -Passed $result.Success -Details "Pay later report works"
    }
    catch {
        Write-TestResult -TestName "GET /reports/pay-later" -Passed $false -Details $_.Exception.Message
    }
} else {
    Write-Host "[SKIP] Reports tests - no auth token" -ForegroundColor Yellow
}

# --------------------------------------------
# 7. Cleanup (delete test product)
# --------------------------------------------
Write-TestHeader "7. Cleanup"

if ($script:Token -and $script:CreatedProductId) {
    try {
        $result = Invoke-ApiRequest -Method "DELETE" -Endpoint "/products/$($script:CreatedProductId)" -Headers (Get-AuthHeaders) -IgnoreError $true
        Write-TestResult -TestName "DELETE /products/{id}" -Passed $result.Success -Details "Test product deleted"
    }
    catch {
        Write-TestResult -TestName "DELETE /products/{id}" -Passed $false -Details $_.Exception.Message
    }
} else {
    Write-Host "[SKIP] Cleanup - no product to delete" -ForegroundColor Yellow
}

# --------------------------------------------
# Summary
# --------------------------------------------
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Passed: $script:PassCount" -ForegroundColor Green
Write-Host "Failed: $script:FailCount" -ForegroundColor $(if ($script:FailCount -gt 0) { "Red" } else { "Green" })
Write-Host "Total:  $($script:PassCount + $script:FailCount)"
Write-Host ""

if ($script:FailCount -gt 0) {
    Write-Host "Some tests FAILED. Check the output above for details." -ForegroundColor Red
    exit 1
} else {
    Write-Host "All tests PASSED!" -ForegroundColor Green
    exit 0
}
