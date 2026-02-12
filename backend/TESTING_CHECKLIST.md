# Complete Testing Checklist - AlfTekPro HRMS

## 🎯 **Testing Strategy**

**Goal**: Validate ALL completed functionality against business requirements with ZERO tolerance for:
- ❌ Garbage code
- ❌ Anti-patterns
- ❌ Deviations from architecture
- ❌ Incomplete implementations

---

## ✅ **PHASE 1: Automated Tests (MUST PASS 100%)**

### Step 1: Run Unit Tests
```bash
cd c:\Users\Admin\Documents\alftekpro\backend

# Run all unit tests
dotnet test tests/AlfTekPro.UnitTests/AlfTekPro.UnitTests.csproj --verbosity detailed

# Expected: 38 tests passed, 0 failed
```

**What This Validates**:
- ✅ BR-LEAVE-001 to BR-LEAVE-004: Leave request business logic
- ✅ BR-ATT-001 to BR-ATT-005: Attendance business logic
- ✅ BR-DEPT-001, BR-DEPT-002: Department hierarchy and deletion
- ✅ BR-ROSTER-001 to BR-ROSTER-003: Roster assignment logic

**Pass Criteria**: ALL tests green ✅

---

### Step 2: Run Integration Tests (CRITICAL - Security)
```bash
# Run multi-tenancy tests
dotnet test tests/AlfTekPro.IntegrationTests/AlfTekPro.IntegrationTests.csproj --verbosity detailed

# Expected: 3 tests passed, 0 failed
```

**What This Validates**:
- ✅ BR-MT-001: Complete tenant data isolation
- ✅ BR-MT-002: Automatic tenant_id injection
- ✅ Cross-tenant access prevention (404, not 403)

**Pass Criteria**: ALL tests green ✅ (CRITICAL - Security requirement)

---

### Step 3: Build & Compile Check
```bash
# Clean build
dotnet clean
dotnet build

# Expected: 0 errors, 0 warnings (warnings acceptable if documented)
```

**What This Validates**:
- ✅ No compilation errors
- ✅ All dependencies resolved
- ✅ Project references correct

**Pass Criteria**: Build succeeds with 0 errors

---

## ✅ **PHASE 2: Manual API Testing (Business Workflows)**

### Prerequisites
```bash
# Start PostgreSQL
docker start alftekpro-postgres

# Verify database
docker exec -i alftekpro-postgres psql -U hrms_user -d alftekpro_hrms -c "\dt"

# Start API
cd src/AlfTekPro.API
dotnet run
# Should start on http://localhost:5000
```

---

### Test Suite 1: Authentication & Multi-Tenancy (CRITICAL)

**Test Case 1.1: Tenant Onboarding**
```powershell
# Run: backend/test-core-hr.ps1
.\test-core-hr.ps1

# OR manually:
$response = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/tenants/onboard" `
  -ContentType "application/json" `
  -Body (@{
    organizationName = "Test Corp"
    subdomain = "testcorp"
    regionId = "00000000-0000-0000-0000-000000000001"
    adminFirstName = "Admin"
    adminLastName = "User"
    adminEmail = "admin@testcorp.com"
    adminPassword = "Test@123456"
    contactPhone = "+971501234567"
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ Response contains: tenantId, adminUserId, subdomain
✓ Database: New tenant + user created
```

**Test Case 1.2: Login & Token Generation**
```powershell
$login = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/auth/login" `
  -ContentType "application/json" `
  -Body (@{
    email = "admin@testcorp.com"
    password = "Test@123456"
  } | ConvertTo-Json)

# Verify:
✓ Status: 200 OK
✓ Response contains: token, refreshToken, expiresAt
✓ Token is valid JWT
✓ Token contains: user_id, tenant_id, role claims
```

**Test Case 1.3: Refresh Token Rotation**
```powershell
# Use refresh token from login
$refresh = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/auth/refresh" `
  -ContentType "application/json" `
  -Body (@{
    refreshToken = $login.data.refreshToken
  } | ConvertTo-Json)

# Verify:
✓ New access token received
✓ New refresh token received (different from original)
✓ Old refresh token invalidated (try using it again → should fail)
```

**Test Case 1.4: Multi-Tenancy Isolation (CRITICAL)**
```powershell
# Create Tenant A with employee
# Create Tenant B with employee
# Login as Tenant A
# Query employees → MUST only see Tenant A employees
# Login as Tenant B
# Query employees → MUST only see Tenant B employees

# CRITICAL: Try to access Tenant A employee ID with Tenant B token
# Expected: 404 Not Found (NOT 403 - prevents info disclosure)
```

---

### Test Suite 2: Core HR Module

**Test Case 2.1: Departments (Hierarchy)**
```powershell
$headers = @{ "Authorization" = "Bearer $token" }

# Create parent department
$engineering = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/departments" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Engineering"
    code = "ENG"
    isActive = $true
  } | ConvertTo-Json)

# Create child department
$backend = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/departments" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Backend Team"
    code = "ENG-BACKEND"
    parentDepartmentId = $engineering.data.id
    isActive = $true
  } | ConvertTo-Json)

# Get hierarchy
$hierarchy = Invoke-RestMethod -Method GET -Uri "http://localhost:5000/api/departments/hierarchy" -Headers $headers

# Verify:
✓ Engineering has children array
✓ Backend Team is nested under Engineering
✓ Parent-child relationship correct
```

**Test Case 2.2: Circular Reference Prevention**
```powershell
# Try to make Engineering's parent = Backend Team (creates cycle)
$response = Invoke-RestMethod -Method PUT -Uri "http://localhost:5000/api/departments/$($engineering.data.id)" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Engineering"
    code = "ENG"
    parentDepartmentId = $backend.data.id  # Creates cycle!
    isActive = $true
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error message: "circular reference"
```

**Test Case 2.3: Designations**
```powershell
$designation = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/designations" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    title = "Senior Software Engineer"
    code = "SSE"
    level = 3
    isActive = $true
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ Level validation (1-100)
✓ Code uniqueness enforced
```

**Test Case 2.4: Locations with Geofencing**
```powershell
$location = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/locations" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Dubai Head Office"
    code = "DXB-HQ"
    address = "Sheikh Zayed Road"
    city = "Dubai"
    country = "UAE"
    latitude = 25.2048
    longitude = 55.2708
    radiusMeters = 100
    isActive = $true
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ hasGeofence = true
✓ Latitude/longitude/radius all saved
```

**Test Case 2.5: Employees with JSONB Dynamic Data**
```powershell
$employee = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/employees" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeCode = "EMP001"
    firstName = "Ahmed"
    lastName = "Al Maktoum"
    email = "ahmed@testcorp.com"
    dateOfBirth = "1990-01-15T00:00:00Z"
    joiningDate = "2024-01-01T00:00:00Z"
    departmentId = $engineering.data.id
    designationId = $designation.data.id
    locationId = $location.data.id
    status = "Active"
    dynamicData = @{
      emirates_id = "784-1990-1234567-1"
      passport_number = "A1234567"
    }
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ Age calculated from dateOfBirth
✓ TenureDays calculated from joiningDate
✓ dynamicData stored as JSONB and returned correctly
```

---

### Test Suite 3: Workforce Management

**Test Case 3.1: Shift Masters**
```powershell
$shift = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/shiftmasters" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Morning Shift"
    code = "MORNING"
    startTime = "09:00:00"
    endTime = "17:00:00"
    gracePeriodMinutes = 15
    totalHours = 8
    isActive = $true
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ startTimeFormatted = "09:00"
✓ endTimeFormatted = "17:00"
✓ Validation: endTime > startTime
```

**Test Case 3.2: Employee Roster Assignment**
```powershell
$roster = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/employeerosters" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    shiftId = $shift.data.id
    effectiveDate = (Get-Date).ToString("yyyy-MM-dd")
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ Roster assigned to employee
✓ daysActive calculated

# Test duplicate prevention
$duplicate = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/employeerosters" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    shiftId = $shift.data.id
    effectiveDate = (Get-Date).ToString("yyyy-MM-dd")  # Same date!
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error: "already has a roster entry"
```

**Test Case 3.3: Clock In with Geofencing**
```powershell
# Clock in WITHIN geofence (100m of office)
$clockIn = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/attendancelogs/clock-in" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    latitude = 25.2048
    longitude = 55.2708
  } | ConvertTo-Json)

# Verify:
✓ Status: 200 OK
✓ clockInWithinGeofence = true
✓ isLate calculated based on shift + grace period
✓ IP address captured

# Try clock in again (same day)
$duplicate = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/attendancelogs/clock-in" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    latitude = 25.2048
    longitude = 55.2708
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error: "already clocked in"
✓ Shows existing clock-in time
```

**Test Case 3.4: Clock In OUTSIDE Geofence**
```powershell
# Create new employee for this test
# Clock in far away (25.3000, 55.3000 - about 13km away)
$clockInFail = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/attendancelogs/clock-in" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee2.data.id
    latitude = 25.3000
    longitude = 55.3000
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error: "outside the allowed geofence"
✓ Error shows: radius (100 meters) and location name
```

**Test Case 3.5: Clock Out & Total Hours**
```powershell
$clockOut = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/attendancelogs/clock-out" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
  } | ConvertTo-Json)

# Verify:
✓ Status: 200 OK
✓ clockOut timestamp set
✓ totalHours calculated (clockOut - clockIn)
✓ Message shows total working hours
```

---

### Test Suite 4: Leave Management

**Test Case 4.1: Leave Types**
```powershell
$leaveType = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leavetypes" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    name = "Annual Leave"
    code = "AL"
    maxDaysPerYear = 20
    isCarryForward = $true
    requiresApproval = $true
    isActive = $true
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ Code validation (uppercase, alphanumeric)
```

**Test Case 4.2: Leave Balance Initialization**
```powershell
# Initialize balances for all employees for current year
$year = (Get-Date).Year
$init = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leavebalances/initialize/$year" `
  -Headers $headers

# Verify:
✓ Status: 200 OK
✓ Response shows: createdCount
✓ Balance created for each employee × leave type
✓ Accrued = maxDaysPerYear, Used = 0
```

**Test Case 4.3: Leave Request (Sufficient Balance)**
```powershell
$leaveRequest = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leaverequests" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    leaveTypeId = $leaveType.data.id
    startDate = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")
    endDate = (Get-Date).AddDays(9).ToString("yyyy-MM-dd")
    reason = "Family vacation"
  } | ConvertTo-Json)

# Verify:
✓ Status: 201 Created
✓ status = "Pending" (requires approval)
✓ daysCount = 3
✓ Balance NOT deducted yet (still pending)
```

**Test Case 4.4: Leave Request (Insufficient Balance)**
```powershell
# Try to request more days than available
$failRequest = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leaverequests" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    leaveTypeId = $leaveType.data.id
    startDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
    endDate = (Get-Date).AddDays(60).ToString("yyyy-MM-dd")  # 31 days - exceeds balance
    reason = "Extended vacation"
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error: "Insufficient leave balance"
✓ Error shows: Requested vs Available days
```

**Test Case 4.5: Overlapping Leave Prevention**
```powershell
# Try to create overlapping leave request
$overlap = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leaverequests" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    employeeId = $employee.data.id
    leaveTypeId = $leaveType.data.id
    startDate = (Get-Date).AddDays(8).ToString("yyyy-MM-dd")  # Overlaps with previous
    endDate = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
    reason = "Another vacation"
  } | ConvertTo-Json) -ErrorAction SilentlyContinue

# Verify:
✓ Status: 400 Bad Request
✓ Error: "overlapping leave request"
```

**Test Case 4.6: Leave Approval & Balance Deduction**
```powershell
# Approve leave request
$approval = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/leaverequests/$($leaveRequest.data.id)/process" `
  -Headers $headers -ContentType "application/json" `
  -Body (@{
    approved = $true
    comments = "Approved"
  } | ConvertTo-Json)

# Verify:
✓ Status: 200 OK
✓ status = "Approved"
✓ approvedBy = current user ID
✓ approvedAt timestamp set

# Check balance was deducted
$balance = Invoke-RestMethod -Method GET `
  -Uri "http://localhost:5000/api/leavebalances/employee/$($employee.data.id)/year/$year" `
  -Headers $headers

# Verify:
✓ used = 3 (deducted)
✓ balance = 17 (20 - 3)
```

---

## ✅ **PHASE 3: Code Quality Review**

### Checklist 1: Architecture Compliance

**Clean Architecture Layers**:
- [ ] Domain: Pure entities, no dependencies ✓
- [ ] Application: DTOs, Interfaces, Validators ✓
- [ ] Infrastructure: Services, DbContext, Repositories ✓
- [ ] API: Controllers, Middleware ✓

**Verify**:
```bash
# Domain should have NO project references
cat src/AlfTekPro.Domain/AlfTekPro.Domain.csproj | grep ProjectReference
# Expected: Empty (no dependencies)

# Application should only reference Domain
cat src/AlfTekPro.Application/AlfTekPro.Application.csproj | grep ProjectReference
# Expected: Only Domain

# Infrastructure should reference Application + Domain
cat src/AlfTekPro.Infrastructure/AlfTekPro.Infrastructure.csproj | grep ProjectReference
# Expected: Application, Domain

# API should reference all layers
cat src/AlfTekPro.API/AlfTekPro.API.csproj | grep ProjectReference
# Expected: Application, Infrastructure
```

---

### Checklist 2: Anti-Pattern Detection

**Common Anti-Patterns to CHECK FOR**:

❌ **Repository Pattern Over EF Core** (Not needed - EF Core is already UoW + Repository)
```csharp
// ANTI-PATTERN (if found):
public interface IEmployeeRepository { }
public class EmployeeRepository : IEmployeeRepository { }

// CORRECT (what we should have):
// Services use DbContext directly
```

❌ **God Classes** (Classes doing too much)
```csharp
// Check: Any service >500 lines? Any class with >10 methods?
```

❌ **Magic Strings/Numbers**
```csharp
// ANTI-PATTERN:
if (user.Role == "Admin") { }  // Magic string

// CORRECT:
if (user.Role == UserRole.Admin) { }  // Enum
```

❌ **Leaky Abstractions**
```csharp
// ANTI-PATTERN:
Task<DbSet<Employee>> GetEmployees();  // Leaking EF Core

// CORRECT:
Task<List<EmployeeResponse>> GetAllEmployeesAsync();  // Returns DTO
```

❌ **Primitive Obsession**
```csharp
// ANTI-PATTERN:
public void SendEmail(string to, string from, string subject) { }

// BETTER:
public void SendEmail(EmailMessage message) { }
```

---

### Checklist 3: SOLID Principles

- [ ] **S** - Single Responsibility: Each class has one reason to change
- [ ] **O** - Open/Closed: Open for extension, closed for modification
- [ ] **L** - Liskov Substitution: Subtypes are substitutable
- [ ] **I** - Interface Segregation: No fat interfaces
- [ ] **D** - Dependency Inversion: Depend on abstractions

**Verify**:
- Services implement single interfaces ✓
- DTOs are immutable where appropriate ✓
- No circular dependencies ✓

---

### Checklist 4: Security Review

**CRITICAL Security Checks**:

- [ ] ✅ Passwords hashed with BCrypt (NEVER plain text)
- [ ] ✅ JWT secrets in configuration (not hardcoded)
- [ ] ✅ SQL injection prevented (EF Core parameterized queries)
- [ ] ✅ XSS prevention (API returns JSON, not HTML)
- [ ] ✅ CORS configured (not allowing all origins in production)
- [ ] ✅ Role-based authorization on sensitive endpoints
- [ ] ✅ Tenant isolation via global query filters
- [ ] ✅ No sensitive data in logs

**Manual Verification**:
```bash
# Check for plain text passwords
grep -r "password.*=" src/ --include="*.cs" | grep -v "PasswordHash"
# Expected: No plain text passwords

# Check for hardcoded secrets
grep -r "\"Secret\".*:" src/ --include="*.cs"
# Expected: Reading from configuration only

# Check CORS config
cat src/AlfTekPro.API/Program.cs | grep -A3 "AddCors"
# Expected: AllowAll for dev only (should be restricted in prod)
```

---

### Checklist 5: Performance Review

- [ ] Async/await used consistently ✓
- [ ] No N+1 query problems (Include() navigation properties) ✓
- [ ] Pagination for large lists (TODO: Add to employee endpoints)
- [ ] Indexes on foreign keys (EF Core creates by default) ✓
- [ ] No Select N+1 in loops ✓

---

### Checklist 6: Code Consistency

**Naming Conventions**:
- [ ] Classes: PascalCase ✓
- [ ] Methods: PascalCase ✓
- [ ] Private fields: _camelCase ✓
- [ ] Constants: UPPER_SNAKE_CASE or PascalCase ✓
- [ ] Async methods: EndWithAsync ✓

**Code Style**:
- [ ] 4-space indentation ✓
- [ ] Braces on new line ✓
- [ ] XML comments on public APIs ✓
- [ ] No unused usings ✓

---

## ✅ **PHASE 4: Database Validation**

### Verify Database State
```bash
# Connect to database
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms

# Check all tables exist
\dt

# Verify row counts
SELECT
  'regions' as table_name, COUNT(*) FROM regions UNION ALL
  SELECT 'tenants', COUNT(*) FROM tenants UNION ALL
  SELECT 'users', COUNT(*) FROM users UNION ALL
  SELECT 'employees', COUNT(*) FROM employees;

# Check tenant isolation
SELECT tenant_id, COUNT(*) as employee_count
FROM employees
GROUP BY tenant_id;
# Should show employees grouped by tenant
```

---

## 📋 **Summary Checklist**

### Must Pass (BLOCKER)
- [ ] ✅ All 41 automated tests pass
- [ ] ✅ Multi-tenancy isolation works (CRITICAL)
- [ ] ✅ Build compiles with 0 errors
- [ ] ✅ No anti-patterns found
- [ ] ✅ Security checks pass

### Should Pass (HIGH)
- [ ] ✅ All manual API tests pass
- [ ] ✅ Database constraints working
- [ ] ✅ Error messages are user-friendly
- [ ] ✅ No code duplication >50 lines

### Nice to Have (MEDIUM)
- [ ] Code coverage >70%
- [ ] Performance: API responds <500ms
- [ ] No compiler warnings

---

## 🎯 **Success Criteria**

**READY FOR NEXT MODULE if**:
✅ All automated tests pass (41/41)
✅ Manual workflows work end-to-end
✅ No critical code quality issues
✅ Security checklist complete
✅ Multi-tenancy verified (P0)

**NOT READY if ANY**:
❌ Tests failing
❌ Anti-patterns found
❌ Security vulnerabilities
❌ Tenant data leakage possible
