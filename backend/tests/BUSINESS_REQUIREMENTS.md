# AlfTekPro HRMS - Business Requirements & Acceptance Criteria

> **Version**: 2.0
> **Last Updated**: 2026-02-12
> **Audience**: QA Team, Product Managers, Developers, Stakeholders
> **Purpose**: Defines 100% of the business rules, functional requirements, and acceptance criteria for the AlfTekPro Multi-Tenant HRMS platform. Tests and QA validation MUST be written against these requirements, NOT against current implementation behavior.

---

## Table of Contents

1. [Multi-Tenancy](#1-multi-tenancy-requirements)
2. [Authentication & Authorization](#2-authentication--authorization-requirements)
3. [Platform Module (Tenants & Regions)](#3-platform-module-requirements)
4. [Core HR - Departments](#4-department-requirements)
5. [Core HR - Designations](#5-designation-requirements)
6. [Core HR - Locations](#6-location-requirements)
7. [Core HR - Employees](#7-employee-requirements)
8. [Core HR - Employee Job History](#8-employee-job-history-requirements)
9. [Shift Management](#9-shift-management-requirements)
10. [Roster Management](#10-roster-management-requirements)
11. [Attendance Management](#11-attendance-management-requirements)
12. [Leave Types](#12-leave-type-requirements)
13. [Leave Balances](#13-leave-balance-requirements)
14. [Leave Requests](#14-leave-request-requirements)
15. [Payroll - Salary Components](#15-salary-component-requirements)
16. [Payroll - Salary Structures](#16-salary-structure-requirements)
17. [Payroll - Payroll Runs](#17-payroll-run-requirements)
18. [Payroll - Payslips](#18-payslip-requirements)
19. [Asset Management](#19-asset-management-requirements)
20. [Action Center (User Tasks)](#20-action-center-user-tasks-requirements)
21. [Dynamic Forms (Form Templates)](#21-dynamic-forms-form-template-requirements)
22. [Data Validation Standards](#22-data-validation-standards)
23. [API Standards & Error Handling](#23-api-standards--error-handling)
24. [Security Requirements](#24-security-requirements)
25. [Performance Requirements](#25-performance-requirements)
26. [Test Priority Matrix](#26-test-priority-matrix)

---

## 1. MULTI-TENANCY REQUIREMENTS

### BR-MT-001: Complete Data Isolation
**Requirement**: Data from one tenant MUST NEVER be visible to another tenant.

**Acceptance Criteria**:
- User from Tenant A cannot see any data (employees, departments, leaves, payroll, etc.) from Tenant B
- All API queries automatically filter by tenant_id extracted from the JWT token
- No explicit tenant filtering is required in application code — isolation is enforced at the database query filter level
- Global query filters are applied to ALL tenant-scoped entities without exception

**Test Scenarios**:
- Create employees in Tenant A and Tenant B
- Login as Tenant A user, query employees endpoint
- MUST return only Tenant A employees
- Tenant B employees MUST NOT appear in any results
- Repeat for all tenant-scoped entities: departments, designations, locations, leave types, leave balances, leave requests, attendance logs, shift masters, employee rosters, salary components, salary structures, payroll runs, payslips, assets, asset assignments, user tasks, form templates

### BR-MT-002: Automatic Tenant ID Injection
**Requirement**: All tenant-scoped entities MUST automatically have tenant_id set during creation.

**Acceptance Criteria**:
- SaveChanges interceptor automatically injects tenant_id from the current tenant context
- No manual tenant_id assignment required in service code
- Creating a tenant-scoped entity without a valid tenant context MUST fail
- tenant_id cannot be overridden by the API consumer to access another tenant's data

**Test Scenarios**:
- Create a department without manually setting tenant_id
- Verify tenant_id is automatically set from JWT context
- Attempt to set tenant_id to a different tenant's ID in the request body
- Verify the system uses the JWT tenant_id, not the request body value

### BR-MT-003: Cross-Tenant Query Prevention
**Requirement**: No API endpoint shall allow querying or modifying data belonging to another tenant.

**Acceptance Criteria**:
- GET by ID: Attempting to fetch an entity belonging to another tenant returns 404 (not 403, to avoid information leakage)
- PUT/DELETE by ID: Attempting to modify/delete an entity belonging to another tenant returns 404
- No endpoint accepts tenant_id as a query parameter or path parameter (except SuperAdmin platform endpoints)

**Test Scenarios**:
- Tenant A creates department with ID X
- Tenant B attempts GET /departments/{X} → 404 Not Found
- Tenant B attempts PUT /departments/{X} → 404 Not Found
- Tenant B attempts DELETE /departments/{X} → 404 Not Found

---

## 2. AUTHENTICATION & AUTHORIZATION REQUIREMENTS

### BR-AUTH-001: JWT Token Authentication
**Requirement**: All protected endpoints require a valid JWT Bearer token.

**Acceptance Criteria**:
- Unauthenticated requests return 401 Unauthorized
- Expired tokens return 401 with `Token-Expired: true` response header
- Valid token allows access to authorized endpoints
- JWT token contains: user_id (sub), email, tenant_id, role
- Token expiry is configurable (default: 60 minutes)
- Clock skew tolerance is zero (no grace period for expired tokens)

**Test Scenarios**:
- Call protected endpoint without token → 401
- Call protected endpoint with malformed token → 401
- Call protected endpoint with expired token → 401 + Token-Expired header
- Call protected endpoint with valid token → 200/success
- Verify token payload contains user_id, email, tenant_id, role claims

### BR-AUTH-002: User Login
**Requirement**: Users authenticate with email and password to receive JWT credentials.

**Acceptance Criteria**:
- Login endpoint is public (no authentication required)
- Valid email + password returns: access token, refresh token, user profile, token expiry
- Invalid email returns generic error (not "email not found" — to prevent enumeration)
- Invalid password returns generic error (not "wrong password")
- Inactive user (IsActive=false) cannot login
- User's LastLogin timestamp is updated on successful login
- Login is scoped to tenant — same email can exist in different tenants

**Test Scenarios**:
- Login with valid credentials → 200 + tokens
- Login with invalid email → 401 "Invalid email or password"
- Login with wrong password → 401 "Invalid email or password"
- Login with inactive user → 401 "Account is inactive"
- Verify LastLogin is updated after successful login
- Verify response includes accessToken, refreshToken, expiresAt, user object

### BR-AUTH-003: Refresh Token Rotation
**Requirement**: Refresh tokens are single-use and rotated on each refresh.

**Acceptance Criteria**:
- Using a valid refresh token generates a new access token + new refresh token
- The old refresh token is immediately revoked (marked with revocation timestamp)
- Attempting to use a revoked refresh token returns 400 error
- Refresh tokens have a configurable expiry (default: 7 days)
- Each refresh token records: created by IP, revoked by IP, replaced by token
- Revocation reason is recorded for audit trail

**Test Scenarios**:
- Login → Get accessToken1 and refreshToken1
- Refresh with refreshToken1 → Get accessToken2 and refreshToken2
- Attempt refresh with refreshToken1 again → 400 "Token has been revoked"
- Refresh with refreshToken2 → Success
- Attempt refresh with expired refreshToken → 400 "Token has expired"

### BR-AUTH-004: Logout
**Requirement**: Users can explicitly logout by revoking their refresh token.

**Acceptance Criteria**:
- Logout endpoint accepts the refresh token to revoke
- Revoked token cannot be used for subsequent refresh attempts
- Logout does not invalidate the current access token (it expires naturally)

**Test Scenarios**:
- Login and get refreshToken
- Logout with refreshToken → 200 Success
- Attempt refresh with revoked refreshToken → 400 Error

### BR-AUTH-005: Role-Based Access Control (RBAC)
**Requirement**: Actions are restricted based on user roles.

**Roles (ordered by privilege level)**:

| Code | Role | Scope | Description |
|------|------|-------|-------------|
| SA | SuperAdmin | Platform-wide | Full access to all tenants, platform management |
| TA | TenantAdmin | Tenant-wide | Full access within their tenant |
| MGR | Manager | Team-scoped | Manage team, approve leaves, regularize attendance |
| PA | PayrollAdmin | Tenant-wide (payroll) | Run payroll, manage salary structures |
| EMP | Employee | Self-scoped | View own data, apply leave, clock in/out |

**Permission Matrix**:

| Resource | Create | Read | Update | Delete | Approve |
|----------|--------|------|--------|--------|---------|
| Tenants | SA | SA | SA | SA | - |
| Regions | SA | All | SA | SA | - |
| Departments | TA | TA, MGR | TA | TA | - |
| Designations | TA | TA, MGR | TA | TA | - |
| Locations | TA | TA, MGR | TA | TA | - |
| Employees | TA | TA, MGR(team), EMP(self) | TA | TA | - |
| Shift Masters | TA | TA, MGR | TA | TA | - |
| Employee Rosters | TA, MGR | TA, MGR | TA, MGR | TA, MGR | - |
| Attendance Logs | EMP(clock) | TA, MGR, EMP(self) | - | - | MGR(regularize) |
| Leave Types | TA | All | TA | TA | - |
| Leave Balances | TA | TA, MGR, EMP(self) | TA | - | - |
| Leave Requests | EMP | TA, MGR(team), EMP(self) | - | - | MGR, TA |
| Salary Components | TA, PA | TA, PA | TA, PA | TA, PA | - |
| Salary Structures | TA, PA | TA, PA | TA, PA | TA, PA | - |
| Payroll Runs | PA | TA, PA | PA | PA(draft only) | TA(publish) |
| Payslips | System | TA, PA, EMP(self) | - | - | - |
| Assets | TA | TA, EMP(own) | TA | TA | - |
| User Tasks | System, MGR | TA, MGR, EMP(own) | - | TA, MGR | EMP(action) |
| Form Templates | SA | All | SA | SA | - |

**Test Scenarios**:
- Employee attempts to create department → 403 Forbidden
- Manager approves leave request for their team → 200
- TenantAdmin creates a leave type → 201
- Employee views own profile → 200
- Employee views another employee's profile → 403
- PayrollAdmin runs payroll → 200
- Manager attempts to run payroll → 403

---

## 3. PLATFORM MODULE REQUIREMENTS

### BR-PLAT-001: Tenant Onboarding
**Requirement**: New tenants (companies) can be onboarded to the platform via a self-service or admin-initiated process.

**Acceptance Criteria**:
- Tenant creation requires: company name, subdomain, region
- Subdomain must be unique across the entire platform
- Subdomain format: lowercase alphanumeric and hyphens only, 3-100 characters
- Tenant is assigned to exactly one region (determines localization, currency, date format)
- An initial admin user is automatically created for the new tenant
- Tenant starts with IsActive=true
- Subscription start date defaults to creation date
- Subscription end date is optional (null = ongoing/lifetime)

**Test Scenarios**:
- Create tenant with valid data → 201 + tenant details + admin user credentials
- Create tenant with duplicate subdomain → 400 "Subdomain already taken"
- Create tenant with invalid region → 400 "Region not found"
- Verify admin user is created with TenantAdmin (TA) role
- Verify tenant is associated with correct region

### BR-PLAT-002: Subdomain Availability Check
**Requirement**: Before onboarding, users can check if a subdomain is available.

**Acceptance Criteria**:
- Check endpoint accepts a subdomain string
- Returns available: true/false
- Case-insensitive comparison
- Reserved subdomains (admin, api, www, app, etc.) are rejected

**Test Scenarios**:
- Check "acme" (not taken) → available: true
- Create tenant with "acme", then check "acme" again → available: false
- Check "ACME" (case-insensitive) → available: false
- Check "admin" (reserved) → available: false

### BR-PLAT-003: Region Management
**Requirement**: The platform supports multiple regions, each defining localization settings.

**Supported Regions** (initial):

| Code | Name | Currency | Date Format | Direction | Language | Timezone |
|------|------|----------|-------------|-----------|----------|----------|
| UAE | United Arab Emirates | AED | dd/MM/yyyy | RTL | Arabic (ar) | Asia/Dubai |
| USA | United States | USD | MM/dd/yyyy | LTR | English (en) | America/New_York |
| IND | India | INR | dd/MM/yyyy | LTR | Hindi (hi) | Asia/Kolkata |

**Acceptance Criteria**:
- Region code is unique across the platform
- Regions are NOT tenant-scoped (they are global platform data)
- Each region defines: code, name, currency code, date format, text direction (RTL/LTR), language code, timezone
- Regions are seeded on first startup (idempotent)
- Only SuperAdmin can manage regions

**Test Scenarios**:
- GET /regions → returns all 3 regions
- Verify each region has correct currency, timezone, direction
- Attempt to create duplicate region code → error

### BR-PLAT-004: Tenant Subscription Management
**Requirement**: Tenants have subscription periods that determine system access.

**Acceptance Criteria**:
- Tenant has subscription_start and subscription_end dates
- subscription_end = null means ongoing/lifetime subscription
- Expired subscription (subscription_end < now) should prevent login with appropriate error
- IsActive flag can be used to manually suspend a tenant

**Test Scenarios**:
- Active tenant with valid subscription → login succeeds
- Tenant with IsActive=false → login fails "Account is inactive"
- Tenant with expired subscription → login fails "Subscription expired" (future enhancement)

---

### BR-PLAT-005: Document Handling
**Requirement**: Secure upload and retrieval of HR documents (offer letters, ID proofs, certificates, etc.).

**Acceptance Criteria**:
- Allowed file types: PDF, JPG, PNG only — reject all others with 400 "Unsupported file type"
- Maximum file size: 5 MB — reject oversized uploads with 400 "File exceeds 5 MB limit"
- Storage: Private Azure Blob Storage container (Azurite emulator in development)
- Files are never publicly accessible; retrieval is via time-limited SAS token URLs (default expiry: 15 minutes)
- Upload endpoint returns a `FileKey` (blob path) which is stored in the Employee's `DynamicData` JSONB field
- Each upload is scoped to the tenant (blob path includes TenantId prefix)
- Delete endpoint soft-deletes the blob reference; actual blob deletion follows a retention policy

**Test Scenarios**:
- Upload valid PDF (< 5 MB) → 200 with FileKey
- Upload .exe file → 400 "Unsupported file type"
- Upload 10 MB file → 400 "File exceeds 5 MB limit"
- Retrieve file with valid FileKey → 200 with time-limited SAS URL
- Retrieve file from another tenant → 403 Forbidden
- Delete file → reference removed from DynamicData, blob marked for deletion

---

## 4. DEPARTMENT REQUIREMENTS

### BR-DEPT-001: Department CRUD
**Requirement**: TenantAdmin can manage departments within their organization.

**Entity Fields**:
- Name (required, max 200 chars)
- Code (required, unique per tenant, max 50 chars)
- Description (optional, max 500 chars)
- ParentDepartmentId (optional — for nested hierarchy)
- HeadUserId (optional — department head)
- IsActive (default: true)

**Acceptance Criteria**:
- Create department with required fields → 201
- Department code must be unique within the tenant
- Different tenants can use the same department code
- Update department name, description, head, parent → 200
- Soft delete sets IsActive=false (does not physically remove)

**Test Scenarios**:
- Create department "Engineering" with code "ENG" → 201
- Create another "Engineering" with code "ENG" in same tenant → 400 "Code already exists"
- Different tenant creates "ENG" → 201 (tenant isolation)
- Update department name → 200
- Delete department → 200 (soft delete, IsActive=false)

### BR-DEPT-002: Department Hierarchy
**Requirement**: Departments can have a parent-child hierarchy (unlimited nesting).

**Acceptance Criteria**:
- Department can set ParentDepartmentId to create hierarchy
- Root departments have ParentDepartmentId = null
- A department CANNOT be its own parent
- Circular references MUST be prevented (A → B → C → A)
- Validation checks the entire ancestor chain

**Test Scenarios**:
- Create Dept A (root) → Success
- Create Dept B, parent = Dept A → Success
- Create Dept C, parent = Dept B → Success (chain: A → B → C)
- Set Dept A parent = Dept C → Error "Circular reference detected"
- Set Dept A parent = Dept A → Error "Department cannot be its own parent"
- Set Dept B parent = null → Success (becomes root)

### BR-DEPT-003: Department Deletion Protection
**Requirement**: Cannot delete department that has active employees assigned.

**Acceptance Criteria**:
- Check if department has any employees with status != Terminated
- If employees exist, return error with count
- Only soft delete (set IsActive=false), never hard delete
- Department with child departments cannot be deleted unless children are reassigned

**Test Scenarios**:
- Department has 5 active employees → Error "Cannot delete department with 5 active employees"
- Transfer all employees to another department → Delete succeeds (soft delete)
- Verify department still exists in DB with IsActive=false

---

## 5. DESIGNATION REQUIREMENTS

### BR-DESG-001: Designation CRUD
**Requirement**: TenantAdmin can manage job designations/titles.

**Entity Fields**:
- Title (required, max 200 chars) — e.g., "Software Engineer", "Manager"
- Code (required, unique per tenant, max 50 chars) — e.g., "SWE", "MGR"
- Description (optional, max 500 chars)
- Level (required, integer) — numeric hierarchy level for org structure
- IsActive (default: true)

**Acceptance Criteria**:
- Create designation with required fields → 201
- Code must be unique within the tenant
- Level determines hierarchy (higher = more senior)
- Soft delete sets IsActive=false

**Test Scenarios**:
- Create "Software Engineer" (code: SWE, level: 3) → 201
- Create duplicate code "SWE" → 400 "Code already exists"
- List designations → returns all active designations
- Soft delete → IsActive=false

### BR-DESG-002: Designation Level Hierarchy
**Requirement**: Designation levels define seniority for org chart and reporting purposes.

**Acceptance Criteria**:
- Level 1 = most junior, higher numbers = more senior
- Multiple designations can share the same level
- Level is used for display ordering and hierarchy visualization

**Test Scenarios**:
- Create: Intern (level 1), Associate (level 2), Engineer (level 3), Sr. Engineer (level 4), Manager (level 5), Director (level 6)
- List sorted by level → ordered from lowest to highest

---

## 6. LOCATION REQUIREMENTS

### BR-LOC-001: Location CRUD
**Requirement**: TenantAdmin can manage office locations.

**Entity Fields**:
- Name (required, max 200 chars) — e.g., "Dubai Head Office"
- Code (required, unique per tenant, max 50 chars) — e.g., "DXB-HQ"
- Address (required, max 500 chars)
- City (optional, max 100 chars)
- State (optional, max 100 chars)
- Country (optional, max 100 chars)
- PostalCode (optional, max 20 chars)
- Latitude (optional, decimal) — GPS coordinate for geofencing
- Longitude (optional, decimal) — GPS coordinate for geofencing
- RadiusMeters (optional, integer, default: 100) — geofence radius
- ContactEmail (optional, max 200 chars)
- ContactPhone (optional, max 50 chars)
- IsActive (default: true)

**Acceptance Criteria**:
- Create location with required fields → 201
- Code must be unique within tenant
- Latitude/Longitude are required for geofencing-enabled locations
- RadiusMeters defines the allowed clock-in radius (in meters)
- Soft delete sets IsActive=false

**Test Scenarios**:
- Create "Dubai Head Office" with GPS coords → 201
- Create duplicate code → 400 "Code already exists"
- Create location without GPS (no geofencing) → 201
- Update radius → 200
- Soft delete → IsActive=false

### BR-LOC-002: Geofencing Configuration
**Requirement**: Each location can define a geofence for attendance validation.

**Acceptance Criteria**:
- Geofence is defined by: latitude, longitude, and radius in meters
- If latitude/longitude are set, geofencing is enabled for that location
- Radius must be a positive integer (minimum: 50 meters, maximum: 10,000 meters)
- Distance calculation uses the Haversine formula (great-circle distance)

**Test Scenarios**:
- Location at (25.2048, 55.2708) with 200m radius
- Employee at (25.2048, 55.2708) → within geofence (0m distance)
- Employee at (25.2050, 55.2710) → within geofence (~28m)
- Employee at (25.3000, 55.3000) → outside geofence (~11km)

---

## 7. EMPLOYEE REQUIREMENTS

### BR-EMP-001: Employee CRUD
**Requirement**: TenantAdmin can manage the full employee lifecycle.

**Entity Fields**:
- EmployeeCode (required, unique per tenant, max 50 chars) — e.g., "EMP-001"
- FirstName (required, max 100 chars)
- LastName (required, max 100 chars)
- Email (required, unique per tenant, max 200 chars)
- Phone (optional, max 20 chars)
- Gender (optional) — "Male", "Female", "Other"
- DateOfBirth (optional)
- JoiningDate (required)
- DepartmentId (optional — FK to Department)
- DesignationId (optional — FK to Designation)
- LocationId (optional — FK to Location)
- ReportingManagerId (optional — self-referencing FK to Employee)
- Status (required) — Active, OnNotice, Terminated, OnLeave
- IsActive (default: true)

**Acceptance Criteria**:
- Create employee with required fields → 201
- EmployeeCode must be unique within tenant
- Email must be unique within tenant
- Department, Designation, Location, ReportingManager (if provided) must exist and belong to same tenant
- Soft delete sets IsActive=false, Status=Terminated

**Test Scenarios**:
- Create employee with valid data → 201
- Create with duplicate code → 400 "Employee code already exists"
- Create with duplicate email → 400 "Email already exists"
- Create with non-existent department → 400 "Department not found"
- Create with department from another tenant → 400 "Department not found"
- Update employee details → 200
- Soft delete employee → IsActive=false, Status=Terminated

### BR-EMP-002: Employee Status Lifecycle
**Requirement**: Employee status follows a defined lifecycle.

**Status Transitions**:
```
Active → OnNotice → Terminated
Active → Terminated (immediate)
Active → OnLeave → Active
```

**Acceptance Criteria**:
- New employees start with Status=Active
- Status change to Terminated sets IsActive=false
- Terminated employees cannot be reactivated
- OnLeave status is informational (leave module manages actual leave)

**Test Scenarios**:
- Create employee → Status=Active, IsActive=true
- Update status to OnNotice → 200
- Update status to Terminated → IsActive=false
- Attempt to update terminated employee → Error "Cannot update terminated employee"

### BR-EMP-003: Reporting Manager Hierarchy
**Requirement**: Employees can have a reporting manager (self-referencing relationship).

**Acceptance Criteria**:
- ReportingManagerId references another Employee in the same tenant
- An employee CANNOT be their own reporting manager
- Circular reporting chains MUST be prevented
- Manager hierarchy is used for: leave approvals, team views, org chart

**Test Scenarios**:
- Set Employee B's manager to Employee A → Success
- Set Employee A's manager to Employee A → Error "Cannot report to self"
- A reports to B, B reports to C, attempt to set C reports to A → Error "Circular reference"

### BR-EMP-004: Employee-Department-Designation Linking
**Requirement**: Employees are linked to departments and designations.

**Acceptance Criteria**:
- Employee can be assigned to exactly one department (optional)
- Employee can have exactly one designation (optional)
- Employee can be assigned to exactly one location (optional)
- Changing department or designation creates a job history record (see BR-JH requirements)

**Test Scenarios**:
- Create employee in Engineering department with SWE designation → 201
- Transfer employee to Finance department → 200 + job history created
- Promote employee to Sr. SWE → 200 + job history created

### BR-EMP-005: Exit Clearance Check
**Requirement**: Employee status cannot be changed to "Terminated" if they have active asset assignments.

**Acceptance Criteria**:
- Before setting status to Terminated, query AssetAssignment for this EmployeeId where IsActive=true
- If count > 0, return 400 Error with list of unreturned assets
- Error message: "Cannot terminate employee with {count} active asset assignment(s). Please process asset returns first."
- This enforces HR compliance — company property must be recovered before exit
- OnNotice status is allowed regardless of asset status (exit process begins, assets returned during notice period)

**Test Scenarios**:
- Employee has 2 active asset assignments (laptop + phone)
- Attempt to set status = Terminated → 400 "Cannot terminate employee with 2 active asset assignment(s). Please process asset returns first."
- Return laptop (1 remaining) → Attempt terminate → 400 (still 1 active)
- Return phone (0 remaining) → Attempt terminate → 200 Success
- Employee with no assets → Terminate directly → 200 Success

### BR-EMP-006: Bulk Employee Import
**Requirement**: Support CSV upload for onboarding multiple employees at once.

**Acceptance Criteria**:
- Accept CSV file upload with employee data rows
- Validation strategy: Report failures per row (partial success allowed) — each row is validated independently
- Duplicate Email/Code checks apply to every row (both against existing DB records AND within the uploaded batch)
- Dynamic Data columns (region-specific fields) in CSV must be parsed and validated against the Region's Form Template schema
- Maximum file size: 5MB
- Maximum rows per upload: 500
- Response includes: total rows, successful count, failed count, and per-row error details for failures
- Successfully imported rows create Employee records with ONBOARDING job history

**Test Scenarios**:
- Upload CSV with 10 valid rows → 200, all 10 imported
- Upload CSV with 8 valid + 2 duplicate emails → 200, 8 imported, 2 failed with error details
- Upload CSV with row containing invalid department code → that row fails, others succeed
- Upload CSV with 2 rows having the same email (intra-batch duplicate) → second row fails
- Upload CSV exceeding 500 rows → 400 "Maximum 500 rows allowed"
- Upload file exceeding 5MB → 400 "Maximum file size is 5MB"
- Upload non-CSV file → 400 "Invalid file format"

---

## 8. EMPLOYEE JOB HISTORY REQUIREMENTS

### BR-JH-001: Automatic Job History Tracking (SCD Type 2)
**Requirement**: Every change to an employee's department, designation, or location creates an audit trail.

**Entity Fields**:
- EmployeeId (FK)
- ChangeType — "ONBOARDING", "TRANSFER", "PROMOTION", "DEMOTION", "LOCATION_CHANGE"
- FromDepartmentId / ToDepartmentId
- FromDesignationId / ToDesignationId
- FromLocationId / ToLocationId
- EffectiveDate
- Reason (optional)
- ChangedByUserId

**Acceptance Criteria**:
- Employee creation generates an "ONBOARDING" history record
- Department change generates a "TRANSFER" history record
- Designation change to a higher level generates a "PROMOTION" record
- Designation change to a lower level generates a "DEMOTION" record
- Location change generates a "LOCATION_CHANGE" record
- History records are immutable (no update/delete)
- History is ordered by EffectiveDate descending

**Test Scenarios**:
- Create employee in Engineering as SWE → Job history: ONBOARDING
- Transfer to Finance → Job history: TRANSFER (from Engineering to Finance)
- Promote to Sr. SWE (level 4 from level 3) → Job history: PROMOTION
- Demote to Associate (level 2 from level 3) → Job history: DEMOTION
- Move to Abu Dhabi office → Job history: LOCATION_CHANGE
- Query history → returns all records ordered by date

---

## 9. SHIFT MANAGEMENT REQUIREMENTS

### BR-SHIFT-001: Shift Master CRUD
**Requirement**: TenantAdmin can define shift patterns.

**Entity Fields**:
- Name (required, max 200 chars) — e.g., "Morning Shift", "Night Shift"
- Code (required, unique per tenant, max 50 chars) — e.g., "MS", "NS"
- StartTime (required, TimeOnly) — e.g., 09:00
- EndTime (required, TimeOnly) — e.g., 17:00
- GraceMinutes (required, integer, 0-120) — late arrival grace period
- IsActive (default: true)

**Acceptance Criteria**:
- Create shift with required fields → 201
- Code must be unique within tenant
- GraceMinutes defines the tolerance before marking an employee as late
- Soft delete sets IsActive=false
- Inactive shifts cannot be assigned to new rosters

**Test Scenarios**:
- Create "Morning Shift" (09:00-17:00, 15 min grace) → 201
- Create duplicate code → 400 "Code already exists"
- Update grace minutes → 200
- Deactivate shift → IsActive=false
- Attempt to use inactive shift in roster → Error "Shift is inactive"

### BR-SHIFT-002: Shift Time Validation
**Requirement**: Shift start and end times must be valid.

**Acceptance Criteria**:
- StartTime and EndTime are required
- Overnight shifts are supported (e.g., 22:00 - 06:00)
- GraceMinutes must be between 0 and 120

**Test Scenarios**:
- Create shift 09:00-17:00 → Success
- Create overnight shift 22:00-06:00 → Success
- Create shift with grace 15 minutes → Success
- Create shift with grace 150 minutes → Error "Grace period must be 0-120 minutes"

---

## 10. ROSTER MANAGEMENT REQUIREMENTS

### BR-ROSTER-001: Employee Roster Assignment
**Requirement**: Managers or TenantAdmins can assign shifts to employees with an effective date.

**Entity Fields**:
- EmployeeId (required, FK to Employee)
- ShiftMasterId (required, FK to ShiftMaster)
- EffectiveDate (required, date)

**Acceptance Criteria**:
- One roster entry per employee per effective date (no duplicates)
- Employee and Shift must exist and belong to the same tenant
- Shift must be active (IsActive=true)
- Effective date determines when the shift assignment takes effect

**Test Scenarios**:
- Assign "Morning Shift" to Employee A effective Jan 1 → 201
- Assign "Night Shift" to Employee A effective Jan 1 → Error "Roster already exists for this date"
- Assign "Night Shift" effective Jan 15 → 201
- Assign inactive shift → Error "Shift is inactive"
- Assign to non-existent employee → Error "Employee not found"

### BR-ROSTER-002: Current Shift Resolution
**Requirement**: The system determines an employee's current shift from the most recent roster entry on or before today.

**Acceptance Criteria**:
- Query rosters where EffectiveDate <= today
- Order by EffectiveDate descending
- First result = current shift
- If no roster entry exists, employee has no assigned shift

**Test Scenarios**:
- Today is Jan 20
- Roster 1: Morning Shift effective Jan 1
- Roster 2: Night Shift effective Jan 15
- Roster 3: Evening Shift effective Feb 1
- Current shift query → "Night Shift" (effective Jan 15 is most recent <= today)

### BR-ROSTER-003: Roster Listing
**Requirement**: View roster assignments with filtering capabilities.

**Acceptance Criteria**:
- List all rosters for a tenant
- Filter by employee
- Filter by date range
- Response includes shift details (name, start time, end time)
- Response includes employee details (name, code)

**Test Scenarios**:
- GET /rosters → returns all roster entries
- GET /rosters?employeeId={id} → returns rosters for specific employee
- Verify response includes nested shift and employee information

---

## 11. ATTENDANCE MANAGEMENT REQUIREMENTS

### BR-ATT-001: Clock-In
**Requirement**: Employees can clock in once per day.

**Acceptance Criteria**:
- First clock-in of the day creates an attendance record with status "Present"
- Records clock-in timestamp and IP address
- Second clock-in attempt on the same day returns error with existing clock-in time
- Date is determined by the employee's tenant timezone (not UTC)

**Test Scenarios**:
- Clock in at 09:00 → 201, status = Present
- Attempt second clock-in at 09:30 → 400 "Already clocked in today at 09:00:00"
- Next day clock-in → 201 (new day)

### BR-ATT-002: Geofencing Validation on Clock-In
**Requirement**: If the employee's assigned location has geofencing enabled, clock-in GPS coordinates must be within the defined radius.

**Acceptance Criteria**:
- Clock-in request includes: latitude, longitude (optional — required if location has geofence)
- Employee's location is determined from their profile
- Distance calculated using Haversine formula
- If distance > location's RadiusMeters → reject with clear error
- If location has no GPS coordinates → skip geofencing check
- If employee has no assigned location → skip geofencing check

**Test Scenarios**:
- Office at (25.2048, 55.2708) with 200m radius
- Clock in from (25.2048, 55.2708) → Success (0m, exact match)
- Clock in from (25.2050, 55.2710) → Success (within 200m)
- Clock in from (25.3000, 55.3000) → Error "Outside office geofence. You are Xm away from {location name} (allowed: 200m)"

### BR-ATT-003: Late Detection Based on Shift
**Requirement**: Employee is automatically marked late if clocking in after shift start time plus grace period.

**Acceptance Criteria**:
- System resolves employee's current shift from the roster
- Late threshold = Shift StartTime + GraceMinutes
- Clock-in BEFORE threshold → IsLate = false
- Clock-in AFTER threshold → IsLate = true, LateByMinutes calculated
- If employee has no assigned shift → no late detection (mark as Present only)

**Test Scenarios**:
- Shift: 09:00-17:00, Grace: 15 minutes (threshold = 09:15)
- Clock in at 08:55 → IsLate=false
- Clock in at 09:10 → IsLate=false (within grace)
- Clock in at 09:15 → IsLate=false (exactly at threshold, within grace)
- Clock in at 09:20 → IsLate=true, LateByMinutes=5
- Clock in at 10:00 → IsLate=true, LateByMinutes=45

### BR-ATT-004: Clock-Out
**Requirement**: Employees can clock out once per day (requires prior clock-in).

**Acceptance Criteria**:
- Cannot clock out without a clock-in record for the same day
- Records clock-out timestamp and IP address
- Cannot clock out twice on the same day
- Total hours calculated as: ClockOut - ClockIn (in hours, decimal)
- Clock-out does not require geofencing

**Test Scenarios**:
- Attempt clock-out without clock-in → 400 "No clock-in record found for today"
- Clock in at 09:00, clock out at 17:00 → TotalHours=8.0
- Clock in at 09:30, clock out at 18:15 → TotalHours=8.75
- Attempt second clock-out → 400 "Already clocked out today at 17:00:00"

### BR-ATT-005: Attendance Regularization
**Requirement**: Managers can regularize late or missed attendance with a reason.

**Acceptance Criteria**:
- Only Manager+ roles can perform regularization
- Only attendance records with status Present and IsLate=true can be regularized
- Regularization requires a reason (minimum 10 characters)
- After regularization: IsLate=false, IsRegularized=true, RegularizationReason saved
- Already regularized records cannot be regularized again
- Employees cannot self-regularize

**Test Scenarios**:
- Employee is late by 30 mins (IsLate=true)
- Employee attempts regularization → 403 Forbidden
- Manager regularizes with reason "Traffic jam on highway" → IsLate=false, IsRegularized=true
- Manager attempts to regularize same record again → 400 "Already regularized"
- Manager attempts regularization with short reason (5 chars) → 400 "Reason must be at least 10 characters"

### BR-ATT-007: Bulk Attendance Approval
**Requirement**: Managers can approve/regularize multiple attendance records in a single operation.

**Acceptance Criteria**:
- Accept a list of attendance log IDs to regularize in one request
- All records must belong to the same tenant
- All records must be in a regularizable state (Present + IsLate=true, not already regularized)
- A single reason applies to all records in the batch
- Atomic operation: all succeed or all fail (if any record is invalid, entire batch is rejected)
- Maximum batch size: 50 records per request
- Only Manager+ roles can perform bulk approval

**Test Scenarios**:
- Manager selects 5 late attendance records, provides reason → 200, all 5 regularized
- Batch includes 1 already-regularized record → 400 "Record {id} is already regularized" (entire batch rejected)
- Batch includes record from another tenant → 400 "Record {id} not found"
- Batch exceeds 50 records → 400 "Maximum 50 records per batch"
- Employee attempts bulk approval → 403 Forbidden

### BR-ATT-006: Attendance Summary
**Requirement**: View attendance records with filtering and summary capabilities.

**Acceptance Criteria**:
- List attendance logs with filters: date range, employee, status
- Each record shows: employee name, date, clock-in time, clock-out time, total hours, status, is late, late by minutes
- Managers see their team's attendance
- Employees see only their own attendance

**Test Scenarios**:
- GET /attendance-logs → returns attendance records for current tenant
- Filter by date range → returns matching records
- Verify each record includes calculated TotalHours
- Employee sees only own records, Manager sees team records

---

## 12. LEAVE TYPE REQUIREMENTS

### BR-LT-001: Leave Type CRUD
**Requirement**: TenantAdmin can define leave types for their organization.

**Entity Fields**:
- Name (required, max 200 chars) — e.g., "Annual Leave", "Sick Leave"
- Code (required, unique per tenant, max 50 chars) — e.g., "AL", "SL"
- MaxDaysPerYear (required, decimal) — maximum entitled days per calendar year
- IsCarryForward (boolean, default: false) — whether unused days roll to next year
- RequiresApproval (boolean, default: true) — whether manager approval is needed
- IsActive (boolean, default: true)

**Acceptance Criteria**:
- Create leave type with required fields → 201
- Code must be unique within tenant
- MaxDaysPerYear must be a positive number
- Different tenants can have different leave types
- Soft delete sets IsActive=false
- Inactive leave types cannot be used for new leave requests

**Test Scenarios**:
- Create "Annual Leave" (code: AL, max: 30, carry: true, approval: true) → 201
- Create "Sick Leave" (code: SL, max: 15, carry: false, approval: true) → 201
- Create "Casual Leave" (code: CL, max: 10, carry: false, approval: false) → 201
- Duplicate code → 400 "Code already exists"
- MaxDaysPerYear = 0 or negative → 400 validation error

---

## 13. LEAVE BALANCE REQUIREMENTS

### BR-LB-001: Annual Balance Initialization
**Requirement**: Leave balances are initialized for all active employees at the start of each year.

**Acceptance Criteria**:
- One balance record per employee per leave type per year
- Accrued = MaxDaysPerYear (for employees who joined before the year)
- Used = 0 (start of year)
- Available = Accrued - Used
- Duplicate balances for same employee/type/year are prevented

**Test Scenarios**:
- Initialize 2026 balances for 10 employees with 3 leave types → 30 balance records
- Verify each balance: Accrued = MaxDaysPerYear, Used = 0
- Attempt to initialize again → Error "Balances already exist for this year"

### BR-LB-002: Pro-Rata Accrual for Mid-Year Joiners
**Requirement**: Employees who join mid-year receive proportional leave balance.

**Formula**: `Accrued = (MaxDaysPerYear / 12) * RemainingMonths`

Where RemainingMonths = (13 - JoiningMonth) for the joining year.

**Acceptance Criteria**:
- Employee joining in January → full entitlement (12/12)
- Employee joining in July → 7/12 of entitlement (Jul-Dec + partial)
- Employee joining in December → 1/12 of entitlement
- Rounded to 2 decimal places

**Test Scenarios**:
- Leave type: 24 days/year
- Employee joins January → Accrued = 24.00 days (24/12 * 12)
- Employee joins April → Accrued = 18.00 days (24/12 * 9)
- Employee joins July → Accrued = 14.00 days (24/12 * 7)
- Employee joins October → Accrued = 8.00 days (24/12 * 4)
- Employee joins December → Accrued = 2.00 days (24/12 * 1)

### BR-LB-003: Balance Deduction on Leave Approval
**Requirement**: Leave balance is deducted ONLY when a leave request is approved, NOT when applied.

**Acceptance Criteria**:
- Pending leave request does NOT affect balance
- Approved leave request increments the Used field
- Rejected leave request does NOT affect balance
- Cancelled leave request reverses the Used deduction (if previously approved)
- Balance update is atomic with approval (same transaction)

**Test Scenarios**:
- Employee has 10 days balance (Accrued=10, Used=0)
- Apply for 3 days → Balance still shows Available=10
- Approve request → Balance: Used=3, Available=7
- Apply for 5 more days, then reject → Balance remains: Used=3, Available=7
- Cancel the approved 3-day leave → Balance: Used=0, Available=10

### BR-LB-004: Carry-Forward Logic
**Requirement**: For leave types with IsCarryForward=true, unused balance rolls to the next year.

**Acceptance Criteria**:
- End of year: CarriedForward = (Accrued - Used) for carry-forward types
- New year balance: Accrued = MaxDaysPerYear + CarriedForward
- Non-carry-forward types reset to MaxDaysPerYear only
- Maximum carry-forward limit may be defined per leave type (future enhancement)

**Test Scenarios**:
- Annual Leave (carry-forward=true, max 30 days)
- Year 2025: Accrued=30, Used=20, Remaining=10
- Year 2026 initialization: Accrued = 30 + 10 = 40

---

## 14. LEAVE REQUEST REQUIREMENTS

### BR-LR-001: Leave Application
**Requirement**: Employees can apply for leave.

**Entity Fields**:
- EmployeeId (required, FK)
- LeaveTypeId (required, FK)
- StartDate (required, date)
- EndDate (required, date)
- Reason (optional, max 1000 chars)
- DaysCount (calculated, decimal)
- Status — Pending, Approved, Rejected, Cancelled
- ApprovedByUserId (null until actioned)
- ApprovalDate (null until actioned)
- ApproverComments (optional, set during approval/rejection)

**Acceptance Criteria**:
- Employee can apply for leave with type, start date, end date, and optional reason
- EndDate must be >= StartDate
- DaysCount is calculated from StartDate to EndDate (inclusive, may exclude weekends — future enhancement)
- Status starts as Pending (unless auto-approved — see BR-LR-004)
- Leave type must be active

**Test Scenarios**:
- Apply for Annual Leave, Jan 15-17 → 201, DaysCount=3, Status=Pending
- Apply with EndDate < StartDate → 400 "End date must be on or after start date"
- Apply with inactive leave type → 400 "Leave type is inactive"

### BR-LR-002: Insufficient Balance Prevention (with Pending Request Awareness)
**Requirement**: Cannot apply for leave if employee has insufficient balance, accounting for already-pending requests.

**Acceptance Criteria**:
- Calculate `ProjectedUsed` = `Used` + `Sum(DaysCount of all PENDING requests for same leave type and year)`
- `AvailableBalance` = `Accrued` - `ProjectedUsed`
- Leave request DaysCount must be <= AvailableBalance
- Error message clearly shows: requested days vs available days (including pending commitment)
- This prevents "double spending" where an employee could submit multiple pending requests exceeding their balance

**Test Scenarios**:
- Employee has 10 days balance (Accrued=10, Used=0, no pending)
- Apply for 5 days → 201 (Available=10, after: ProjectedUsed=5, Available=5)
- Apply for another 5 days → 201 (Available=5, after: ProjectedUsed=10, Available=0)
- Apply for 1 more day → 400 "Insufficient leave balance. Requested: 1 day, Available: 0 days"
- First request gets approved (Used=5, pending=5) → Available still = 0
- First request gets rejected (Used=0, pending=5) → Available = 5
- Apply for 3 days → 201 (Available=5)

### BR-LR-003: Overlapping Leave Prevention
**Requirement**: Cannot have overlapping leave requests for the same employee.

**Acceptance Criteria**:
- Check for date overlap with existing non-rejected requests (Pending or Approved)
- Overlap means: existing.StartDate <= new.EndDate AND existing.EndDate >= new.StartDate
- Rejected and Cancelled requests are excluded from overlap check
- Error message indicates the conflicting date range

**Test Scenarios**:
- Employee has approved leave: Jan 10-15
- Apply for Jan 12-17 → 400 "Overlapping leave request exists (Jan 10-15)"
- Apply for Jan 16-20 → 201 (no overlap)
- Apply for Jan 5-9 → 201 (no overlap)
- Previous leave gets rejected → Apply for Jan 10-15 again → 201 (rejected leaves don't count)

### BR-LR-004: Auto-Approval for No-Approval-Required Types
**Requirement**: Leave types with RequiresApproval=false are auto-approved immediately.

**Acceptance Criteria**:
- When RequiresApproval=false, leave status is set to Approved on creation
- Leave balance is deducted immediately
- No manager action required
- Auto-approval is noted in the system (ApprovedByUserId = null, to indicate system)

**Test Scenarios**:
- Create leave type "Comp Off" with RequiresApproval=false
- Apply for Comp Off → Status = Approved immediately
- Verify balance is deducted immediately
- No approval record (ApprovedByUserId = null)

### BR-LR-005: Leave Approval/Rejection
**Requirement**: Managers can approve or reject pending leave requests.

**Acceptance Criteria**:
- Only Manager+ roles can approve/reject
- Only Pending requests can be approved/rejected
- Approval: Status → Approved, balance deducted, ApprovedByUserId set, ApprovalDate set
- Rejection: Status → Rejected, no balance change, ApproverComments required
- Approved/Rejected requests cannot be re-actioned

**Test Scenarios**:
- Manager approves pending request → Status=Approved, balance deducted
- Manager rejects with comment "Insufficient team coverage" → Status=Rejected
- Attempt to approve already-approved request → 400 "Leave request is not in Pending status"
- Attempt to reject already-rejected request → 400 "Leave request is not in Pending status"
- Employee attempts to approve → 403 Forbidden

### BR-LR-006: Leave Cancellation
**Requirement**: Employees can cancel their own pending or approved leave requests.

**Acceptance Criteria**:
- Only the employee who created the request can cancel it
- Pending requests: cancel without balance impact
- Approved requests: cancel and reverse the balance deduction
- Rejected/Cancelled requests cannot be cancelled again
- Cancellation of future-dated approved leave is allowed; past-dated may be restricted

**Test Scenarios**:
- Cancel pending request → Status=Cancelled, no balance change
- Cancel approved request → Status=Cancelled, Used decremented
- Cancel already-cancelled request → 400 "Leave request is already cancelled"
- Different employee attempts cancel → 403 Forbidden

---

## 15. SALARY COMPONENT REQUIREMENTS

### BR-SC-001: Salary Component CRUD
**Requirement**: Payroll Admins can define salary components (earnings and deductions).

**Entity Fields**:
- Name (required, max 200 chars) — e.g., "Basic Salary", "House Rent Allowance"
- Code (required, unique per tenant, max 50 chars) — e.g., "BASIC", "HRA"
- Type — "Earning" or "Deduction"
- CalculationType — "FIXED" (flat amount) or "PERCENT" (percentage of another component)
- IsTaxable (boolean, default: false)
- IsActive (boolean, default: true)

**Acceptance Criteria**:
- Create component with required fields → 201
- Code must be unique within tenant (case-insensitive)
- Code format: uppercase letters, numbers, hyphens, underscores only (2-50 chars)
- Type must be exactly "Earning" or "Deduction"
- CalculationType must be exactly "FIXED" or "PERCENT"
- Soft delete sets IsActive=false

**Test Scenarios**:
- Create "Basic Salary" (code: BASIC, type: Earning, calc: FIXED) → 201
- Create "HRA" (code: HRA, type: Earning, calc: PERCENT) → 201
- Create "Provident Fund" (code: PF, type: Deduction, calc: PERCENT) → 201
- Duplicate code "BASIC" → 400 "Salary component with code 'BASIC' already exists"
- Different tenant creates "BASIC" → 201 (tenant isolation)

### BR-SC-002: Salary Component Deletion Protection
**Requirement**: Cannot delete salary component that is used in any salary structure.

**Acceptance Criteria**:
- Check if component ID is referenced in any SalaryStructure.ComponentsJson
- If referenced, return error indicating the component is in use
- Soft delete (IsActive=false) instead of hard delete
- Inactive components remain in existing structures but warn during payroll calculation

**Test Scenarios**:
- Create component "BASIC", use it in a salary structure
- Attempt delete → 400 "Cannot delete salary component that is used in salary structures"
- Remove from all structures → delete succeeds (soft delete)

---

## 16. SALARY STRUCTURE REQUIREMENTS

### BR-SS-001: Salary Structure CRUD
**Requirement**: Payroll Admins can define salary structures that combine multiple components.

**Entity Fields**:
- Name (required, max 200 chars) — e.g., "Standard Package - L3"
- ComponentsJson (required, JSONB) — array of component assignments with amounts/percentages
- IsActive (boolean, default: true)

**ComponentsJson Format**:
```json
[
  { "componentId": "guid", "amount": 5000, "percentage": null },
  { "componentId": "guid", "amount": null, "percentage": 40 }
]
```

**Acceptance Criteria**:
- Create structure with name and components array → 201
- All referenced component IDs must exist and belong to the same tenant
- All referenced components must be active (IsActive=true)
- FIXED components have amount set, percentage null
- PERCENT components have percentage set, amount null
- Structure name should be descriptive but uniqueness is not enforced

**Test Scenarios**:
- Create structure with BASIC (5000 fixed) + HRA (40% of BASIC) → 201
- Create with non-existent component ID → 400 "Component not found"
- Create with inactive component → 400 "Component is inactive"
- Update structure to change amounts → 200

### BR-SS-002: Two-Pass Salary Calculation
**Requirement**: Salary is calculated in two passes: fixed components first, then percentage-based.

**Calculation Logic**:
1. **Pass 1**: Sum all FIXED Earning components → Base Gross
2. **Pass 2**: Calculate PERCENT components as percentage of Base Gross
3. **Total Gross** = Sum of all Earning amounts (fixed + percentage)
4. **Total Deductions** = Sum of all Deduction amounts (fixed + percentage)
5. **Net Pay** = Total Gross - Total Deductions (minimum 0)

**Test Scenarios**:
- BASIC = 10,000 (FIXED, Earning)
- HRA = 40% (PERCENT, Earning) → 4,000
- Transport = 500 (FIXED, Earning)
- Total Gross = 10,000 + 4,000 + 500 = 14,500
- PF = 12% (PERCENT, Deduction) → 1,740
- Tax = 1,000 (FIXED, Deduction)
- Total Deductions = 1,740 + 1,000 = 2,740
- Net Pay = 14,500 - 2,740 = 11,760

---

## 17. PAYROLL RUN REQUIREMENTS

### BR-PR-001: Payroll Run Creation
**Requirement**: PayrollAdmin can initiate a payroll run for a specific month/year.

**Entity Fields**:
- Month (required, 1-12)
- Year (required, e.g., 2026)
- Status — Draft, Processing, Completed, Published, Rejected
- TotalGross (calculated after processing)
- TotalDeductions (calculated after processing)
- TotalNet (calculated after processing)
- EmployeeCount (calculated after processing)
- ProcessedAt (timestamp when processing completed)
- ProcessedByUserId (user who initiated processing)

**Acceptance Criteria**:
- Only one active payroll run per month/year per tenant
- Initial status = Draft
- Month must be 1-12, Year must be reasonable (current year ± 1)
- PayrollAdmin+ role required

**Test Scenarios**:
- Create payroll run for January 2026 → 201, Status=Draft
- Create another run for January 2026 → 400 "Payroll run already exists for this period"
- Create run for February 2026 → 201 (different month)
- Different tenant creates January 2026 → 201 (tenant isolation)

### BR-PR-002: Payroll Run Status Workflow
**Requirement**: Payroll run follows a strict status progression.

**Status Transitions**:
```
Draft → Processing → Completed → Published
Draft → Rejected (cancel before processing)
Completed → Rejected (reject after review)
```

**Status Rules**:

| Status | Can Edit | Can Delete | Can Process | Can Publish | Payslips Visible |
|--------|----------|------------|-------------|-------------|------------------|
| Draft | Yes | Yes | Yes | No | No |
| Processing | No | No | No | No | No |
| Completed | No | No | Yes (re-process) | Yes | No |
| Published | No | No | No | No | Yes (to employees) |
| Rejected | No | No | No | No | No |

**Acceptance Criteria**:
- Draft: Editable, deletable, can be processed
- Processing: System-only status during payslip generation (no user actions)
- Completed: Review state — can publish or reject, can re-process (amendment)
- Published: Terminal state — immutable, payslips visible to employees
- Rejected: Terminal state — allows creating a new run for the same period

**Test Scenarios**:
- Create run → Status=Draft
- Delete draft run → Success
- Process run → Status changes: Draft → Processing → Completed
- Attempt delete of completed run → 400 "Cannot delete processed payroll run"
- Publish completed run → Status=Published
- Attempt modify published run → 400 "Cannot modify published payroll run"
- Reject completed run → Status=Rejected
- Create new run for same month → 201 (previous was rejected)

### BR-PR-003: Payroll Processing
**Requirement**: Processing a payroll run generates payslips for all active employees.

**Processing Steps**:
1. Get all active employees for the tenant
2. For each employee:
   a. Find their salary structure
   b. Calculate working days for the month
   c. Calculate present days from attendance records
   d. Apply two-pass salary calculation (fixed → percentage)
   e. Apply pro-rata based on present days vs working days
   f. Generate payslip record with component breakdown

**Pro-Rata Formula**:
```
Pro-Rated Amount = (Monthly Amount / Working Days) * Present Days
```

**Acceptance Criteria**:
- All active employees are included
- Employees without a salary structure are skipped (with warning)
- Working days = total days in month - weekends (future: - holidays)
- Present days = attendance count + approved paid leave days
- Each payslip has a breakdown stored as JSON
- Processing is atomic — all payslips generated in single transaction

**Test Scenarios**:
- 5 active employees, all have salary structures
- Month has 22 working days
- Employee A: 22 present days → full salary
- Employee B: 20 present days → (salary/22) * 20
- Employee C: no salary structure → skipped with warning
- Verify payslip count = 4 (excluding skipped)

### BR-PR-004: One Run Per Month Enforcement
**Requirement**: Only one active payroll run per month/year/tenant combination.

**Acceptance Criteria**:
- Unique constraint on (tenant_id, month, year) for non-rejected runs
- Rejected runs don't block creation of new runs for the same period
- Published runs permanently occupy the month slot

**Test Scenarios**:
- Run A for Jan 2026 (Draft) → Create Run B for Jan 2026 → Error
- Reject Run A → Create Run B for Jan 2026 → Success
- Publish Run B → Create Run C for Jan 2026 → Error (published slot occupied)

---

### BR-PR-005: Full & Final (FnF) Settlement
**Requirement**: Calculate and generate a final settlement for exiting employees.

**Trigger**: Employee status changes to `OnNotice` or `Terminated`.

**Acceptance Criteria**:
- **Leave Encashment**: Unused leave balance × daily rate (GrossSalary / 30)
- **Pro-rata Salary**: Salary calculated up to Last Working Day (LWD)
- **Asset Recovery Deductions**: If unreturned assets exist, their estimated value is deducted (or flagged for manual resolution per BR-EMP-005)
- **Pending Expense Reimbursements**: Any approved but unpaid reimbursements are added to the settlement
- **Output**: A Payslip record with Type = "Settlement" linked to the employee
- **Post-Processing**: Once FnF payslip is published, employee status automatically updates to `Exited`
- Only PayrollAdmin or TenantAdmin can initiate FnF processing

**Test Scenarios**:
- Employee with 5 unused leave days, daily rate 500 → Leave encashment = 2500
- Employee LWD is 15th of month → Pro-rata salary = GrossSalary × (15/30)
- Employee has unreturned laptop (value 50000) → Deduction line item of 50000 or block with warning
- FnF payslip published → Employee status changes to Exited
- Non-admin user attempts FnF → 403 Forbidden
- FnF for employee who is already Exited → 400 "Employee already exited"

---

## 18. PAYSLIP REQUIREMENTS

### BR-PS-001: Payslip Generation
**Requirement**: Payslips are generated automatically during payroll processing.

**Entity Fields**:
- PayrollRunId (FK)
- EmployeeId (FK)
- Month, Year
- WorkingDays (integer)
- PresentDays (integer)
- GrossEarnings (decimal)
- TotalDeductions (decimal)
- NetPay (decimal)
- BreakdownJson (JSONB — detailed component breakdown)

**BreakdownJson Format**:
```json
{
  "earnings": [
    { "componentName": "Basic Salary", "amount": 10000 },
    { "componentName": "HRA", "amount": 4000 }
  ],
  "deductions": [
    { "componentName": "Provident Fund", "amount": 1200 }
  ]
}
```

**Acceptance Criteria**:
- One payslip per employee per payroll run
- NetPay = GrossEarnings - TotalDeductions (minimum 0, never negative)
- BreakdownJson contains complete component-level detail
- Payslips are read-only after generation

**Test Scenarios**:
- Process payroll → payslips created for each employee
- Verify GrossEarnings = sum of earnings in breakdown
- Verify TotalDeductions = sum of deductions in breakdown
- Verify NetPay = GrossEarnings - TotalDeductions
- Verify NetPay >= 0

### BR-PS-002: Payslip Immutability
**Requirement**: Payslips CANNOT be modified once generated.

**Acceptance Criteria**:
- No UPDATE endpoint for payslips
- No DELETE endpoint for payslips
- Corrections are handled via re-processing the entire payroll run
- Re-processing creates new payslips (old ones remain for audit trail)

**Test Scenarios**:
- Generate payslip → attempt direct update → no API endpoint exists
- Error in calculation → re-process run → new payslips generated
- Verify both original and corrected payslips exist in history

### BR-PS-003: Payslip Visibility
**Requirement**: Employees can view their own payslips only after the payroll run is Published.

**Acceptance Criteria**:
- Draft/Processing/Completed runs: payslips visible only to PayrollAdmin and TenantAdmin
- Published runs: payslips visible to the respective employee
- Employee can only see their own payslips, never other employees'

**Test Scenarios**:
- Payroll run in Completed status → Employee queries payslips → empty result
- Publish payroll run → Employee queries payslips → sees own payslip
- Employee A queries Employee B's payslip → 403 or empty result

---

## 19. ASSET MANAGEMENT REQUIREMENTS

### BR-ASSET-001: Asset CRUD
**Requirement**: TenantAdmin can track company assets (laptops, phones, furniture, etc.).

**Entity Fields**:
- AssetCode (required, unique per tenant, max 50 chars) — e.g., "LT-001"
- AssetType (required, max 100 chars) — e.g., "Laptop", "Mobile", "Chair"
- Make (optional, max 100 chars) — e.g., "Dell", "Apple"
- Model (optional, max 200 chars) — e.g., "Latitude 5520"
- SerialNumber (optional, max 200 chars)
- PurchaseDate (optional)
- PurchasePrice (optional, decimal >= 0)
- Status — "Available", "Assigned", "InRepair", "Retired"
- IsActive (default: true)

**Acceptance Criteria**:
- Create asset with required fields → 201
- AssetCode must be unique within tenant
- Status defaults to "Available"
- PurchasePrice must be >= 0 if provided

**Test Scenarios**:
- Create laptop "LT-001" → 201, Status=Available
- Create duplicate code "LT-001" → 400 "Asset code already exists"
- Different tenant creates "LT-001" → 201 (tenant isolation)
- Update asset details → 200

### BR-ASSET-002: Asset Assignment (Issue to Employee)
**Requirement**: Assets can be issued to employees.

**Assignment Fields**:
- AssetId (FK)
- EmployeeId (FK)
- AssignedDate (auto-set to current date)
- AssignedCondition (required) — "New", "Good", "Fair", "Poor"
- ReturnedDate (null until returned)
- ReturnedCondition (null until returned)
- ReturnNotes (null until returned)
- IsActive (true while assigned)

**Acceptance Criteria**:
- Asset must be in "Available" status to be assigned
- Cannot assign an already-assigned asset
- Assignment changes asset status to "Assigned"
- Employee must exist and belong to the same tenant
- AssignedCondition is required at time of assignment

**Test Scenarios**:
- Assign available laptop to Employee A → 201, asset Status=Assigned
- Attempt assign same laptop to Employee B → 400 "Asset is already assigned"
- Assign laptop in "InRepair" status → 400 "Asset is not available for assignment"
- Assign to non-existent employee → 400 "Employee not found"

### BR-ASSET-003: Asset Return
**Requirement**: Assigned assets can be returned.

**Acceptance Criteria**:
- Only currently assigned assets (with active assignment) can be returned
- ReturnedDate is auto-set to current date
- ReturnedCondition is required
- ReturnNotes is optional
- Assignment IsActive set to false
- Asset status changes back to "Available"

**Test Scenarios**:
- Return assigned laptop with condition "Good" → 200, asset Status=Available
- Attempt return of unassigned asset → 400 "Asset is not currently assigned"
- Verify assignment record shows: ReturnedDate, ReturnedCondition, IsActive=false

### BR-ASSET-004: Asset Assignment History
**Requirement**: Full history of asset assignments is maintained.

**Acceptance Criteria**:
- Each assign/return cycle creates a history record
- History shows: who it was assigned to, when, in what condition, when returned, return condition
- History is read-only
- Sorted by assigned date descending

**Test Scenarios**:
- Assign to Employee A → Return → Assign to Employee B → Return
- Query history → returns 2 records
- First record: Employee B (most recent)
- Second record: Employee A

### BR-ASSET-005: Asset Deletion Protection
**Requirement**: Cannot hard-delete an asset that is currently assigned.

**Acceptance Criteria**:
- Assigned assets → soft delete to "Retired" status (cannot be physically deleted)
- Available/InRepair assets → soft delete to "Retired" status
- Delete returns success but asset remains with Status=Retired

**Test Scenarios**:
- Delete available asset → Status=Retired
- Delete assigned asset → Status=Retired (or error "Cannot delete assigned asset" — depends on policy)
- Asset with Status=Retired no longer appears in active listings

---

## 20. ACTION CENTER (USER TASKS) REQUIREMENTS

### BR-TASK-001: Task Creation
**Requirement**: The system generates tasks for users when actions are required (approvals, reviews, etc.).

**Entity Fields**:
- OwnerUserId (required, FK to User) — who needs to action this task
- Title (required, max 500 chars) — e.g., "Approve leave request for John Doe"
- EntityType (required, max 100 chars) — "LEAVE", "ATTENDANCE", "ASSET", "NOTIFICATION"
- EntityId (required, GUID) — ID of the related entity
- Status — "Pending", "Completed", "Dismissed"
- Priority — "Low", "Normal", "High", "Urgent"
- DueDate (optional)
- ActionUrl (optional) — deep link to the entity
- ActionedAt (null until actioned)

**Acceptance Criteria**:
- Task is created with Status=Pending
- Owner user must exist in the same tenant
- EntityType categorizes the task source
- Priority affects ordering (Urgent > High > Normal > Low)
- Tasks can be created by the system (automatic) or by managers (manual)

**Test Scenarios**:
- Create task for manager to approve leave → 201, Status=Pending
- Create task with non-existent owner → 400 "Owner user not found"
- Verify task appears in owner's pending list

### BR-TASK-002: Task Actions (Complete/Dismiss)
**Requirement**: Task owners can complete or dismiss their pending tasks.

**Acceptance Criteria**:
- "Complete" action: Status → Completed, ActionedAt set
- "Dismiss" action: Status → Dismissed, ActionedAt set
- Only Pending tasks can be actioned
- Only the task owner can action the task (or TenantAdmin)
- Invalid actions are rejected

**Test Scenarios**:
- Complete pending task → Status=Completed, ActionedAt set
- Dismiss pending task → Status=Dismissed, ActionedAt set
- Attempt to complete already-completed task → 400 "Only Pending tasks can be actioned"
- Submit invalid action "Archive" → 400 "Action must be 'Complete' or 'Dismiss'"

### BR-TASK-003: Pending Task List
**Requirement**: Users can view their pending tasks, ordered by priority and due date.

**Acceptance Criteria**:
- Filter by owner user
- Only show Pending status tasks
- Ordered by: Priority (Urgent first) → DueDate (earliest first) → CreatedAt
- Include overdue flag: DueDate < today
- Include entity details for context

**Test Scenarios**:
- User has 5 pending tasks (2 Urgent, 2 Normal, 1 Low)
- Query pending tasks → returns 5, ordered by priority
- Task with DueDate yesterday → IsOverdue=true
- Task with no DueDate → IsOverdue=false

### BR-TASK-004: Task Deletion
**Requirement**: TenantAdmin can delete tasks.

**Acceptance Criteria**:
- Only TenantAdmin+ can delete tasks
- Deletion is a hard delete (task removed from database)
- Returns success if found, not-found if doesn't exist

**Test Scenarios**:
- Admin deletes task → 204 No Content
- Delete non-existent task → 404 or false
- Employee attempts delete → 403 Forbidden

---

## 21. DYNAMIC FORMS (FORM TEMPLATE) REQUIREMENTS

### BR-FORM-001: Form Template CRUD
**Requirement**: SuperAdmin can define dynamic form schemas per region and module.

**Entity Fields**:
- RegionId (required, FK to Region)
- Module (required, max 100 chars) — "Employee", "Leave", "Payroll", etc.
- SchemaJson (required, JSONB) — form field definitions
- IsActive (boolean, default: true)

**SchemaJson Format**:
```json
{
  "fields": [
    {
      "key": "emirates_id",
      "label": "Emirates ID",
      "type": "text",
      "required": true,
      "validation": { "pattern": "^[0-9]{15}$" },
      "section": "Documents",
      "order": 1
    },
    {
      "key": "blood_group",
      "label": "Blood Group",
      "type": "dropdown",
      "required": false,
      "options": ["A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"],
      "section": "Personal Info",
      "order": 2
    }
  ]
}
```

**Acceptance Criteria**:
- Create template with region, module, and schema → 201
- Only one active template per region+module combination
- Duplicate region+module → 400 "Template already exists for this region and module"
- SchemaJson is stored as JSONB (flexible, queryable)
- Templates are NOT tenant-scoped — they are shared across all tenants in a region
- Only SuperAdmin can create/update/delete

**Test Scenarios**:
- Create UAE Employee template → 201
- Create another UAE Employee template → 400 "Template already exists"
- Create USA Employee template → 201 (different region)
- Create UAE Leave template → 201 (different module)

### BR-FORM-002: Schema Retrieval by Region + Module
**Requirement**: Frontend can fetch the active form schema for a specific region and module.

**Acceptance Criteria**:
- GET schema by regionId and module name
- Returns the active template's SchemaJson
- If no active template exists → 404
- Used by frontend to dynamically render forms

**Test Scenarios**:
- GET schema for UAE + Employee → returns SchemaJson with UAE-specific fields (Emirates ID, etc.)
- GET schema for USA + Employee → returns SchemaJson with USA-specific fields (SSN, etc.)
- GET schema for non-existent combination → 404

### BR-FORM-003: Region-Specific Fields
**Requirement**: Different regions require different form fields.

**UAE-Specific Fields** (examples):
- Emirates ID (15 digits)
- Visa Type
- Labor Card Number
- WPS (Wage Protection System) details

**USA-Specific Fields** (examples):
- Social Security Number (SSN)
- W-4 Form details
- I-9 Verification

**India-Specific Fields** (examples):
- Aadhaar Number (12 digits)
- PAN Number
- UAN (Universal Account Number) for PF
- ESI Number

**Acceptance Criteria**:
- Each region can have completely different field sets
- Field types supported: text, number, date, dropdown, checkbox, textarea, file
- Fields can be required or optional
- Fields can have validation rules (regex patterns, min/max, etc.)
- Fields are grouped by sections

---

## 22. DATA VALIDATION STANDARDS

### BR-VAL-001: Input Validation via FluentValidation
**Requirement**: All API requests are validated before processing.

**Acceptance Criteria**:
- FluentValidation is auto-wired to validate all incoming requests
- Invalid requests return 400 with detailed validation errors
- Response format: `{ "success": false, "message": "Validation failed", "errors": {...} }`
- Validation runs before any business logic or database queries

### BR-VAL-002: Email Uniqueness (Per Tenant)
**Requirement**: Email addresses must be unique within a tenant.

**Test Scenarios**:
- Create user/employee with email@test.com → Success
- Create another with email@test.com in same tenant → 400 "Email already exists"
- Create email@test.com in different tenant → Success (tenant isolation)

### BR-VAL-003: Code Uniqueness (Per Tenant)
**Requirement**: All code fields (department code, designation code, employee code, leave type code, shift code, asset code, salary component code, location code) must be unique within their tenant.

**Test Scenarios**:
- Create entity with code "XYZ" → Success
- Create another with code "XYZ" in same tenant → 400 "Code already exists"
- Create "XYZ" in different tenant → Success

### BR-VAL-004: Date Range Validation
**Requirement**: End date must be >= start date wherever date ranges are used.

**Applies To**: Leave requests, subscriptions, salary effective dates, payroll periods

**Test Scenarios**:
- StartDate=Jan 15, EndDate=Jan 10 → 400 "End date must be on or after start date"
- StartDate=Jan 15, EndDate=Jan 15 → Success (same-day range)
- StartDate=Jan 15, EndDate=Jan 20 → Success

### BR-VAL-005: Required Field Validation
**Requirement**: All required fields must be present and non-empty.

**Acceptance Criteria**:
- Missing required fields → 400 with field-level error messages
- Empty strings for required string fields → 400
- Null values for required non-nullable fields → 400
- Error message format: `"FieldName is required"`

### BR-VAL-006: String Length Validation
**Requirement**: String fields must respect maximum length constraints.

**Standard Lengths**:
- Name/Title fields: max 200 chars
- Code fields: max 50 chars
- Description fields: max 500 chars
- Email fields: max 200 chars
- Address fields: max 500 chars
- Notes/Reason fields: max 1000 chars

---

## 23. API STANDARDS & ERROR HANDLING

### BR-API-001: Consistent Response Format
**Requirement**: All API responses follow the ApiResponse<T> envelope format.

**Success Response**:
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... }
}
```

**Error Response**:
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

**Acceptance Criteria**:
- All endpoints return the envelope format
- Success operations: success=true, data contains result
- Error operations: success=false, message describes the error
- HTTP status codes are appropriate (see BR-API-002)

### BR-API-002: HTTP Status Codes
**Requirement**: Standard HTTP status codes are used consistently.

| Status | Usage |
|--------|-------|
| 200 OK | Successful GET, PUT |
| 201 Created | Successful POST (resource created) |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Validation error, business rule violation |
| 401 Unauthorized | Missing/invalid/expired JWT token |
| 403 Forbidden | Insufficient permissions (RBAC) |
| 404 Not Found | Resource not found (or cross-tenant access blocked) |
| 500 Internal Server Error | Unexpected server error |

### BR-API-003: Global Exception Handling
**Requirement**: All unhandled exceptions are caught and returned as structured JSON responses.

**Exception Mapping**:

| Exception Type | HTTP Status | Response |
|---------------|-------------|----------|
| InvalidOperationException | 400 | Business rule violation message |
| ArgumentException | 400 | Argument validation message |
| UnauthorizedAccessException | 401 | "Unauthorized access" |
| KeyNotFoundException | 404 | Entity not found message |
| All other exceptions | 500 | "An unexpected error occurred. Please try again later." |

**Acceptance Criteria**:
- No stack traces leaked to API consumers in production
- All errors logged server-side with full stack trace
- Consistent JSON format even for unhandled exceptions
- Content-Type is always application/json for error responses

### BR-API-004: CORS Configuration
**Requirement**: API supports Cross-Origin Resource Sharing for frontend consumption.

**Acceptance Criteria**:
- AllowAnyOrigin (development) / specific origins (production)
- AllowAnyMethod (GET, POST, PUT, DELETE, OPTIONS)
- AllowAnyHeader (Authorization, Content-Type, etc.)

---

## 24. SECURITY REQUIREMENTS

### BR-SEC-001: Password Hashing
**Requirement**: All passwords MUST be hashed using BCrypt before storage. Plain text passwords are NEVER stored.

**Acceptance Criteria**:
- BCrypt with minimum 10 salt rounds
- Password hash is stored in `password_hash` column
- Original password is never recoverable
- Login compares input password against stored BCrypt hash

**Test Scenarios**:
- Create user with password "Test@123"
- Query database → password_hash starts with "$2a$" (BCrypt prefix)
- Login with "Test@123" → Success
- Login with "wrong" → Failure
- Password_hash is never returned in API responses

### BR-SEC-002: SQL Injection Prevention
**Requirement**: All database queries use parameterized queries via EF Core.

**Acceptance Criteria**:
- No raw SQL strings with user input concatenation
- All queries go through EF Core's LINQ → SQL parameterization
- Special characters in inputs are safely escaped

**Test Scenarios**:
- Search with input "'; DROP TABLE employees; --" → No SQL injection, returns safe error
- Input with special chars: `<script>alert('xss')</script>` → stored literally, not executed

### BR-SEC-003: Sensitive Data Protection
**Requirement**: Sensitive fields are never exposed in API responses.

**Protected Fields**:
- password_hash → NEVER returned in any API response
- Refresh tokens → only returned during login/refresh operations
- JWT secret → server-side only
- Database connection strings → server-side only

### BR-SEC-004: Rate Limiting
**Requirement**: API endpoints are rate-limited to prevent abuse.

**Acceptance Criteria**:
- Login endpoint: max 10 attempts per minute per IP
- General API: max 100 requests per minute per user
- Exceeded limits return 429 Too Many Requests

---

## 25. PERFORMANCE REQUIREMENTS

### BR-PERF-001: Pagination
**Requirement**: All list endpoints support pagination.

**Acceptance Criteria**:
- Default page size: 50 items
- Maximum page size: 100 items
- Support parameters: page (default: 1), pageSize (default: 50)
- Response includes: items, totalCount, page, pageSize, totalPages

### BR-PERF-002: Database Indexing
**Requirement**: Critical query paths are indexed.

**Required Indexes**:
- All tenant_id columns (critical for row-level isolation)
- Unique indexes: tenant_id + code for all code fields
- Foreign keys: all FK columns
- Query optimization: attendance (employee_id, date), leave requests (status), users (email)

### BR-PERF-003: Response Times
**Requirement**: API endpoints respond within acceptable time limits.

| Operation | Target P95 |
|-----------|-----------|
| Simple CRUD (GET/POST/PUT) | < 200ms |
| List with pagination | < 500ms |
| Payroll processing (per employee) | < 2s |
| Login/Auth | < 300ms |
| Complex queries (reports) | < 1s |

---

## 26. TEST PRIORITY MATRIX

### Priority Levels

| Priority | Category | Business Impact | Examples |
|----------|----------|-----------------|----------|
| **P0 (Critical)** | Multi-tenancy, Authentication | Data breach, security failure | Tenant isolation, JWT validation, password hashing |
| **P1 (High)** | Leave balance, Attendance accuracy, Payroll calculation | Financial/legal liability | Balance deduction, pro-rata salary, overtime calc |
| **P2 (Medium)** | CRUD operations, Geofencing, Roster management | Core functionality | Department CRUD, shift assignment, asset tracking |
| **P3 (Low)** | Action center, Form templates, UI polish | User experience | Task notifications, dynamic forms, sorting |

### Test Coverage Goals

| Test Type | Coverage Target | Scope |
|-----------|----------------|-------|
| **Unit Tests** | 80% code coverage | Business logic in service layer |
| **Integration Tests** | 100% of P0 + P1 endpoints | HTTP-based API testing |
| **Contract Tests** | 100% API schema | OpenAPI spec validation |
| **E2E Tests** | 10 critical user journeys | Full workflow testing |

### Critical User Journeys (E2E)

1. **Tenant Onboarding**: Create tenant → Create admin → Login → Setup departments
2. **Employee Onboarding**: Create employee → Assign department → Assign shift → First clock-in
3. **Leave Workflow**: Apply leave → Manager approves → Balance deducted → Employee sees updated balance
4. **Attendance Day**: Clock-in (geofenced) → Late detection → Clock-out → Total hours calculated
5. **Attendance Regularization**: Late clock-in → Manager regularizes → Late flag cleared
6. **Payroll Cycle**: Create salary components → Create structure → Create run → Process → Publish → Employee views payslip
7. **Asset Lifecycle**: Create asset → Assign to employee → Return → Assign to another → View history
8. **Multi-Tenant Isolation**: Create same data in 2 tenants → Verify complete isolation
9. **Token Lifecycle**: Login → Access API → Token expires → Refresh → Continue → Logout
10. **Employee Transfer**: Change department → Verify job history → Change designation → Verify promotion history

---

## Appendix A: Entity Reference

| Module | Entity | Tenant-Scoped | Key Fields |
|--------|--------|---------------|------------|
| Platform | Tenant | No | name, subdomain, regionId, isActive, subscriptionStart, subscriptionEnd |
| Platform | Region | No | code, name, currencyCode, dateFormat, direction, languageCode, timezone |
| Platform | User | Yes (by tenantId) | email, firstName, lastName, passwordHash, role, isActive |
| Platform | RefreshToken | No (by userId) | token, userId, expiresAt, revokedAt |
| Core HR | Department | Yes | name, code, description, parentDepartmentId, headUserId, isActive |
| Core HR | Designation | Yes | title, code, description, level, isActive |
| Core HR | Location | Yes | name, code, address, city, country, lat, lng, radiusMeters, isActive |
| Core HR | Employee | Yes | employeeCode, firstName, lastName, email, joiningDate, status, departmentId, designationId, locationId, reportingManagerId |
| Core HR | EmployeeJobHistory | Yes | employeeId, changeType, fromDept/toDept, fromDesig/toDesig, effectiveDate |
| Shifts | ShiftMaster | Yes | name, code, startTime, endTime, graceMinutes, isActive |
| Shifts | EmployeeRoster | Yes | employeeId, shiftMasterId, effectiveDate |
| Attendance | AttendanceLog | Yes | employeeId, date, clockIn, clockOut, totalHours, isLate, lateByMinutes, isRegularized |
| Leave | LeaveType | Yes | name, code, maxDaysPerYear, isCarryForward, requiresApproval, isActive |
| Leave | LeaveBalance | Yes | employeeId, leaveTypeId, year, accrued, used |
| Leave | LeaveRequest | Yes | employeeId, leaveTypeId, startDate, endDate, daysCount, status, approvedByUserId |
| Payroll | SalaryComponent | Yes | name, code, type, calculationType, isTaxable, isActive |
| Payroll | SalaryStructure | Yes | name, componentsJson, isActive |
| Payroll | PayrollRun | Yes | month, year, status, totalGross, totalDeductions, totalNet, employeeCount |
| Payroll | Payslip | Yes | payrollRunId, employeeId, month, year, workingDays, presentDays, grossEarnings, totalDeductions, netPay, breakdownJson |
| Assets | Asset | Yes | assetCode, assetType, make, model, serialNumber, purchaseDate, purchasePrice, status |
| Assets | AssetAssignment | Yes | assetId, employeeId, assignedDate, returnedDate, assignedCondition, returnedCondition, isActive |
| Workflow | UserTask | Yes | ownerUserId, title, entityType, entityId, status, priority, dueDate, actionUrl |
| Forms | FormTemplate | Yes | regionId, module, schemaJson, isActive |

---

## Appendix B: Role Codes Reference

| Code | Full Name | Description |
|------|-----------|-------------|
| SA | SuperAdmin | Platform-level administrator, manages all tenants |
| TA | TenantAdmin | Tenant-level administrator, full access within their company |
| MGR | Manager | Team manager, approves leaves, manages rosters |
| PA | PayrollAdmin | Payroll specialist, manages salary and payroll processing |
| EMP | Employee | Regular employee, self-service access |

---

## Appendix C: Status Enums Reference

**EmployeeStatus**: `Active`, `OnNotice`, `Terminated`, `OnLeave`

**LeaveRequestStatus**: `Pending`, `Approved`, `Rejected`, `Cancelled`

**AttendanceStatus**: `Present`, `Absent`, `HalfDay`, `OnLeave`, `Holiday`, `Weekend`

**PayrollRunStatus**: `Draft`, `Processing`, `Completed`, `Published`, `Rejected`

**AssetStatus**: `Available`, `Assigned`, `InRepair`, `Retired`

**UserTaskStatus**: `Pending`, `Completed`, `Dismissed`

**UserTaskPriority**: `Low`, `Normal`, `High`, `Urgent`

---

**Document Version**: 2.0
**Last Updated**: 2026-02-12
**Maintained By**: Product & Engineering Team
**Review Cycle**: Updated with every sprint, reviewed quarterly
**Approvals**: Product Manager, QA Lead, Engineering Lead
