#!/usr/bin/env pwsh
# Comprehensive Core HR Testing Script
# Tests: Auth → Tenant Onboarding → Regions → Departments → Designations → Locations → Employees

$BaseUrl = "http://localhost:5000"
$ErrorActionPreference = "Continue"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  AlfTekPro HRMS - Core HR Test Suite" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Test results
$Results = @{
    Passed = 0
    Failed = 0
    Tests = @()
}

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Body,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200
    )

    Write-Host "Testing: $Name" -ForegroundColor Yellow

    try {
        $params = @{
            Uri = "$BaseUrl$Url"
            Method = $Method
            ContentType = "application/json"
            Headers = $Headers
        }

        if ($Body) {
            $params.Body = $Body
        }

        $response = Invoke-WebRequest @params -UseBasicParsing
        $statusCode = $response.StatusCode
        $content = $response.Content | ConvertFrom-Json

        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✅ PASS - Status: $statusCode" -ForegroundColor Green
            $Results.Passed++
            $Results.Tests += @{ Name = $Name; Status = "PASS"; Response = $content }
            return $content
        } else {
            Write-Host "  ❌ FAIL - Expected: $ExpectedStatus, Got: $statusCode" -ForegroundColor Red
            $Results.Failed++
            $Results.Tests += @{ Name = $Name; Status = "FAIL"; Error = "Status code mismatch" }
            return $null
        }
    }
    catch {
        Write-Host "  ❌ FAIL - Error: $($_.Exception.Message)" -ForegroundColor Red
        $Results.Failed++
        $Results.Tests += @{ Name = $Name; Status = "FAIL"; Error = $_.Exception.Message }
        return $null
    }
}

Write-Host "[Step 1] Testing Regions Endpoint (Public)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$regions = Test-Endpoint -Name "GET /api/regions" -Method "GET" -Url "/api/regions"

if ($regions -and $regions.data) {
    $uaeRegion = $regions.data | Where-Object { $_.code -eq "UAE" } | Select-Object -First 1
    $regionId = $uaeRegion.id
    Write-Host "  Found UAE Region ID: $regionId" -ForegroundColor Gray
}
Write-Host ""

Write-Host "[Step 2] Tenant Onboarding" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$checkDomain = Test-Endpoint -Name "Check subdomain availability" `
    -Method "GET" `
    -Url "/api/tenants/check-domain/testcorp"

$onboardBody = @{
    organizationName = "Test Corporation"
    subdomain = "testcorp"
    regionId = $regionId
    adminFirstName = "Admin"
    adminLastName = "User"
    adminEmail = "admin@testcorp.com"
    adminPassword = "Test@123456"
    contactPhone = "+971501234567"
} | ConvertTo-Json

$tenant = Test-Endpoint -Name "POST /api/tenants/onboard" `
    -Method "POST" `
    -Url "/api/tenants/onboard" `
    -Body $onboardBody `
    -ExpectedStatus 201

if ($tenant -and $tenant.data) {
    $tenantId = $tenant.data.tenantId
    Write-Host "  Created Tenant ID: $tenantId" -ForegroundColor Gray
}
Write-Host ""

Write-Host "[Step 3] Authentication" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$loginBody = @{
    email = "admin@testcorp.com"
    password = "Test@123456"
} | ConvertTo-Json

$loginResponse = Test-Endpoint -Name "POST /api/auth/login" `
    -Method "POST" `
    -Url "/api/auth/login" `
    -Body $loginBody

if ($loginResponse -and $loginResponse.data) {
    $token = $loginResponse.data.token
    $refreshToken = $loginResponse.data.refreshToken
    Write-Host "  Obtained JWT Token" -ForegroundColor Gray

    $authHeaders = @{
        "Authorization" = "Bearer $token"
    }
}
Write-Host ""

Write-Host "[Step 4] Departments (CRUD + Hierarchy)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$deptBody = @{
    name = "Engineering"
    code = "ENG"
    description = "Engineering Department"
    isActive = $true
} | ConvertTo-Json

$dept = Test-Endpoint -Name "POST /api/departments" `
    -Method "POST" `
    -Url "/api/departments" `
    -Body $deptBody `
    -Headers $authHeaders `
    -ExpectedStatus 201

if ($dept -and $dept.data) {
    $deptId = $dept.data.id
    Write-Host "  Created Department ID: $deptId" -ForegroundColor Gray
}

$subDeptBody = @{
    name = "Backend Team"
    code = "ENG-BACKEND"
    parentDepartmentId = $deptId
    description = "Backend Development Team"
    isActive = $true
} | ConvertTo-Json

$subDept = Test-Endpoint -Name "POST /api/departments (sub-dept)" `
    -Method "POST" `
    -Url "/api/departments" `
    -Body $subDeptBody `
    -Headers $authHeaders `
    -ExpectedStatus 201

if ($subDept -and $subDept.data) {
    $subDeptId = $subDept.data.id
    Write-Host "  Created Sub-Department ID: $subDeptId" -ForegroundColor Gray
}

$deptList = Test-Endpoint -Name "GET /api/departments" `
    -Method "GET" `
    -Url "/api/departments" `
    -Headers $authHeaders

$deptHierarchy = Test-Endpoint -Name "GET /api/departments/hierarchy" `
    -Method "GET" `
    -Url "/api/departments/hierarchy" `
    -Headers $authHeaders

Write-Host ""

Write-Host "[Step 5] Designations (CRUD)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$designationBody = @{
    title = "Senior Software Engineer"
    code = "SSE"
    level = 3
    description = "Senior engineer with 5+ years experience"
    isActive = $true
} | ConvertTo-Json

$designation = Test-Endpoint -Name "POST /api/designations" `
    -Method "POST" `
    -Url "/api/designations" `
    -Body $designationBody `
    -Headers $authHeaders `
    -ExpectedStatus 201

if ($designation -and $designation.data) {
    $designationId = $designation.data.id
    Write-Host "  Created Designation ID: $designationId" -ForegroundColor Gray
}

$designationList = Test-Endpoint -Name "GET /api/designations" `
    -Method "GET" `
    -Url "/api/designations" `
    -Headers $authHeaders

Write-Host ""

Write-Host "[Step 6] Locations (CRUD + Geofencing)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$locationBody = @{
    name = "Dubai Head Office"
    code = "DXB-HQ"
    address = "Sheikh Zayed Road"
    city = "Dubai"
    country = "United Arab Emirates"
    postalCode = "12345"
    latitude = 25.2048
    longitude = 55.2708
    radiusMeters = 100
    contactPhone = "+971501234567"
    contactEmail = "dubai@testcorp.com"
    isActive = $true
} | ConvertTo-Json

$location = Test-Endpoint -Name "POST /api/locations" `
    -Method "POST" `
    -Url "/api/locations" `
    -Body $locationBody `
    -Headers $authHeaders `
    -ExpectedStatus 201

if ($location -and $location.data) {
    $locationId = $location.data.id
    $hasGeofence = $location.data.hasGeofence
    Write-Host "  Created Location ID: $locationId" -ForegroundColor Gray
    Write-Host "  Geofencing Enabled: $hasGeofence" -ForegroundColor Gray
}

$locationList = Test-Endpoint -Name "GET /api/locations" `
    -Method "GET" `
    -Url "/api/locations" `
    -Headers $authHeaders

Write-Host ""

Write-Host "[Step 7] Employees (CRUD + JSONB Dynamic Data)" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$employeeBody = @{
    employeeCode = "EMP001"
    firstName = "Ahmed"
    lastName = "Al Maktoum"
    email = "ahmed@testcorp.com"
    phone = "+971501234567"
    dateOfBirth = "1990-01-15T00:00:00Z"
    gender = "Male"
    joiningDate = "2024-01-01T00:00:00Z"
    departmentId = $deptId
    designationId = $designationId
    locationId = $locationId
    status = "Active"
    dynamicData = @{
        emirates_id = "784-1990-1234567-1"
        passport_number = "A1234567"
        visa_expiry = "2025-12-31"
        labor_card = "LC123456"
    }
} | ConvertTo-Json

$employee = Test-Endpoint -Name "POST /api/employees" `
    -Method "POST" `
    -Url "/api/employees" `
    -Body $employeeBody `
    -Headers $authHeaders `
    -ExpectedStatus 201

if ($employee -and $employee.data) {
    $employeeId = $employee.data.id
    $age = $employee.data.age
    $tenureDays = $employee.data.tenureDays
    Write-Host "  Created Employee ID: $employeeId" -ForegroundColor Gray
    Write-Host "  Calculated Age: $age years" -ForegroundColor Gray
    Write-Host "  Calculated Tenure: $tenureDays days" -ForegroundColor Gray

    if ($employee.data.dynamicData) {
        Write-Host "  JSONB Dynamic Data Stored ✅" -ForegroundColor Gray
        $employee.data.dynamicData | ConvertTo-Json | Write-Host -ForegroundColor DarkGray
    }
}

$employeeList = Test-Endpoint -Name "GET /api/employees" `
    -Method "GET" `
    -Url "/api/employees" `
    -Headers $authHeaders

$employeeByCode = Test-Endpoint -Name "GET /api/employees/code/EMP001" `
    -Method "GET" `
    -Url "/api/employees/code/EMP001" `
    -Headers $authHeaders

Write-Host ""

Write-Host "[Step 8] Test Filters" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$empByDept = Test-Endpoint -Name "GET /api/employees?departmentId=$deptId" `
    -Method "GET" `
    -Url "/api/employees?departmentId=$deptId" `
    -Headers $authHeaders

$empByStatus = Test-Endpoint -Name "GET /api/employees?status=Active" `
    -Method "GET" `
    -Url "/api/employees?status=Active" `
    -Headers $authHeaders

Write-Host ""

Write-Host "[Step 9] Update Operations" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

$updateDeptBody = @{
    name = "Engineering (Updated)"
    code = "ENG"
    description = "Engineering Department - Updated"
    isActive = $true
} | ConvertTo-Json

$updatedDept = Test-Endpoint -Name "PUT /api/departments/$deptId" `
    -Method "PUT" `
    -Url "/api/departments/$deptId" `
    -Body $updateDeptBody `
    -Headers $authHeaders

Write-Host ""

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Test Results Summary" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total Tests: $($Results.Passed + $Results.Failed)" -ForegroundColor White
Write-Host "Passed: $($Results.Passed)" -ForegroundColor Green
Write-Host "Failed: $($Results.Failed)" -ForegroundColor Red
Write-Host ""

if ($Results.Failed -eq 0) {
    Write-Host "🎉 ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  SOME TESTS FAILED" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Failed Tests:" -ForegroundColor Red
    $Results.Tests | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Error)" -ForegroundColor Red
    }
    exit 1
}
