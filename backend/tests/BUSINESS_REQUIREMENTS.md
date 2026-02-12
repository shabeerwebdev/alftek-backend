# AlfTekPro HRMS - Business Requirements & Acceptance Criteria

This document defines the core business rules and functional requirements that the system MUST satisfy.
Tests are written to validate these requirements, NOT to validate what the code currently does.

---

## 1. MULTI-TENANCY REQUIREMENTS

### BR-MT-001: Complete Data Isolation
**Requirement**: Data from one tenant MUST NEVER be visible to another tenant.

**Acceptance Criteria**:
- User from Tenant A cannot see employees from Tenant B
- API queries automatically filter by tenant_id
- No explicit tenant filtering required in application code
- Database level isolation via global query filters

**Test Scenarios**:
- ✓ Create employees in Tenant A and Tenant B
- ✓ Login as Tenant A user
- ✓ Query employees endpoint
- ✓ MUST return only Tenant A employees (count should match)
- ✓ Tenant B employees MUST NOT appear in results

### BR-MT-002: Tenant ID Injection
**Requirement**: All tenant-scoped entities MUST automatically have tenant_id set during creation.

**Acceptance Criteria**:
- SaveChanges interceptor auto-injects tenant_id
- No manual tenant_id assignment in services
- Creating entity without tenant_id context should fail

**Test Scenarios**:
- ✓ Create department without manually setting tenant_id
- ✓ Verify tenant_id is automatically set from JWT context
- ✓ Verify cannot override tenant_id to access other tenant's data

---

## 2. AUTHENTICATION & AUTHORIZATION REQUIREMENTS

### BR-AUTH-001: JWT Token Authentication
**Requirement**: All protected endpoints require valid JWT token.

**Acceptance Criteria**:
- Unauthenticated requests return 401 Unauthorized
- Expired tokens return 401 with Token-Expired header
- Valid token allows access to authorized endpoints

**Test Scenarios**:
- ✓ Call protected endpoint without token → 401
- ✓ Call protected endpoint with expired token → 401
- ✓ Call protected endpoint with valid token → 200/success
- ✓ Token must contain user_id and tenant_id claims

### BR-AUTH-002: Role-Based Access Control
**Requirement**: Actions are restricted based on user roles.

**Acceptance Criteria**:
- SuperAdmin: Full access to all tenants
- TenantAdmin: Full access within their tenant
- Manager: Can approve leave, manage rosters, regularize attendance
- Employee: Can view own data, apply for leave, clock in/out

**Test Scenarios**:
- ✓ Employee cannot create departments → 403 Forbidden
- ✓ Manager can approve leave requests → 200
- ✓ TenantAdmin can create leave types → 201
- ✓ SuperAdmin can access all tenants → 200

### BR-AUTH-003: Refresh Token Rotation
**Requirement**: Refresh tokens are single-use and rotated on each refresh.

**Acceptance Criteria**:
- Using refresh token creates new access + refresh token
- Old refresh token is marked as revoked
- Using revoked refresh token returns 400 error

**Test Scenarios**:
- ✓ Login → Get token1 and refreshToken1
- ✓ Refresh with refreshToken1 → Get token2 and refreshToken2
- ✓ Try to refresh again with refreshToken1 → 400 (already used)
- ✓ Refresh with refreshToken2 → Success

---

## 3. LEAVE MANAGEMENT REQUIREMENTS

### BR-LEAVE-001: Insufficient Balance Prevention
**Requirement**: Cannot approve leave request if employee has insufficient balance.

**Acceptance Criteria**:
- Leave request creation checks available balance
- Available balance = Accrued - Used
- Request for more days than available must fail with clear error

**Test Scenarios**:
- ✓ Employee has 10 days annual leave (5 used, 5 remaining)
- ✓ Apply for 3 days leave → Success (within balance)
- ✓ Apply for 7 days leave → Error "Insufficient balance" (exceeds remaining)
- ✓ Error message shows: Requested vs Available days

### BR-LEAVE-002: Overlapping Leave Prevention
**Requirement**: Cannot have overlapping leave requests for same employee.

**Acceptance Criteria**:
- Leave dates are checked for overlap (start/end inclusive)
- Only non-rejected requests are considered for overlap
- Clear error message indicating conflicting dates

**Test Scenarios**:
- ✓ Employee has approved leave: Jan 10-15
- ✓ Apply for Jan 12-17 → Error "Overlapping leave"
- ✓ Apply for Jan 16-20 → Success (no overlap)
- ✓ Previous leave was rejected → New request allowed even if dates overlap

### BR-LEAVE-003: Balance Deduction on Approval
**Requirement**: Leave balance is deducted ONLY when leave is approved, not when applied.

**Acceptance Criteria**:
- Pending request does not affect balance
- Approved request deducts from Used balance
- Rejected request does not affect balance
- Balance update is atomic with approval

**Test Scenarios**:
- ✓ Employee has 10 days balance (0 used)
- ✓ Apply for 3 days → Balance still shows 10 available
- ✓ Approve request → Balance now shows 7 available (3 used)
- ✓ Apply for another 5 days, then reject → Balance remains 7 available

### BR-LEAVE-004: Auto-Approval for No-Approval-Required Types
**Requirement**: Leave types with RequiresApproval=false are auto-approved.

**Acceptance Criteria**:
- Status immediately set to Approved
- Balance deducted immediately on submission
- No manager approval needed

**Test Scenarios**:
- ✓ Create leave type "Comp Off" with RequiresApproval=false
- ✓ Apply for Comp Off → Status = Approved immediately
- ✓ Balance deducted immediately
- ✓ No approval action required

---

## 4. ATTENDANCE REQUIREMENTS

### BR-ATT-001: Single Clock-In Per Day
**Requirement**: Employee can clock in only once per day.

**Acceptance Criteria**:
- First clock-in of the day creates attendance record
- Second clock-in attempt on same day returns error
- Error message shows existing clock-in time

**Test Scenarios**:
- ✓ Clock in at 9:00 AM → Success
- ✓ Attempt clock in at 9:30 AM → Error "Already clocked in at 09:00:00"
- ✓ Next day clock in → Success (new day)

### BR-ATT-002: Geofencing Validation
**Requirement**: Clock-in location must be within defined radius of office location.

**Acceptance Criteria**:
- Employee's location has latitude, longitude, radius defined
- Clock-in request includes GPS coordinates
- Distance calculated using Haversine formula
- Request rejected if outside radius with clear error

**Test Scenarios**:
- ✓ Office at (25.2048, 55.2708) with 100m radius
- ✓ Clock in from (25.2048, 55.2708) → Success (exact location)
- ✓ Clock in from (25.2050, 55.2710) → Success (within 100m)
- ✓ Clock in from (25.3000, 55.3000) → Error "Outside geofence"
- ✓ Error shows required location and radius

### BR-ATT-003: Late Detection Based on Shift
**Requirement**: Employee is marked late if clocking in after shift start + grace period.

**Acceptance Criteria**:
- Get employee's current shift from roster
- Shift start time + grace period = late threshold
- Clock-in after threshold sets IsLate=true and calculates minutes late
- Grace period configurable per shift (0-120 minutes)

**Test Scenarios**:
- ✓ Shift: 09:00-17:00, Grace: 15 mins
- ✓ Clock in at 08:55 → On time (IsLate=false)
- ✓ Clock in at 09:10 → On time (within grace)
- ✓ Clock in at 09:20 → Late (IsLate=true, LateByMinutes=5)
- ✓ Clock in at 09:45 → Late (LateByMinutes=30)

### BR-ATT-004: Clock-Out Validation
**Requirement**: Can only clock out if already clocked in same day.

**Acceptance Criteria**:
- Clock-out requires existing clock-in record
- Clock-out sets ClockOut timestamp and IP
- Cannot clock out twice
- Total hours calculated as ClockOut - ClockIn

**Test Scenarios**:
- ✓ Attempt clock-out without clock-in → Error "No clock-in record"
- ✓ Clock in at 09:00, clock out at 17:00 → Success, TotalHours=8.0
- ✓ Attempt second clock-out → Error "Already clocked out at 17:00:00"

### BR-ATT-005: Attendance Regularization
**Requirement**: Managers can regularize late attendance with reason.

**Acceptance Criteria**:
- Only pending/late attendance can be regularized
- Requires reason (min 10 chars)
- Clears late flag after regularization
- Only Manager+ roles can regularize

**Test Scenarios**:
- ✓ Employee late by 30 mins → IsLate=true
- ✓ Employee attempts regularization → 403 Forbidden
- ✓ Manager regularizes with reason → IsLate=false, Reason saved
- ✓ Already regularized attendance → Error "Already regularized"

---

## 5. ROSTER & SHIFT REQUIREMENTS

### BR-ROSTER-001: No Duplicate Roster on Same Date
**Requirement**: Employee cannot have two roster entries on same effective date.

**Acceptance Criteria**:
- One roster entry per employee per effective date
- Error message shows existing roster date

**Test Scenarios**:
- ✓ Assign "Morning Shift" to Employee A effective Jan 1
- ✓ Attempt assign "Night Shift" to Employee A effective Jan 1 → Error
- ✓ Assign "Night Shift" effective Jan 15 → Success

### BR-ROSTER-002: Current Roster Calculation
**Requirement**: Employee's current shift is the most recent roster entry <= today.

**Acceptance Criteria**:
- Query rosters where EffectiveDate <= Today
- Order by EffectiveDate DESC
- Take first record

**Test Scenarios**:
- ✓ Today is Jan 20
- ✓ Roster 1: Morning Shift effective Jan 1
- ✓ Roster 2: Night Shift effective Jan 15
- ✓ Roster 3: Evening Shift effective Jan 25
- ✓ Current roster query returns "Night Shift" (effective Jan 15)

### BR-ROSTER-003: Inactive Shift Prevention
**Requirement**: Cannot assign inactive shift to employee.

**Acceptance Criteria**:
- Shift must have IsActive=true
- Clear error if attempting to assign inactive shift

**Test Scenarios**:
- ✓ Mark shift as inactive
- ✓ Attempt roster assignment → Error "Shift is inactive"

---

## 6. DEPARTMENT HIERARCHY REQUIREMENTS

### BR-DEPT-001: Circular Reference Prevention
**Requirement**: Department hierarchy cannot have circular references.

**Acceptance Criteria**:
- Department cannot be its own parent
- Department cannot have ancestor as child
- Validation checks entire parent chain

**Test Scenarios**:
- ✓ Dept A → Dept B → Dept C (valid chain)
- ✓ Set Dept C parent to Dept A → Error (creates cycle)
- ✓ Set Dept A parent to Dept C → Error (creates cycle)
- ✓ Set Dept B parent to null → Success

### BR-DEPT-002: Department Deletion with Employees
**Requirement**: Cannot delete department that has active employees.

**Acceptance Criteria**:
- Check if department has employees (EmployeeCount > 0)
- Return error with count of employees
- Soft delete only (IsActive=false)

**Test Scenarios**:
- ✓ Department has 5 employees
- ✓ Attempt delete → Error "Department has 5 employees"
- ✓ Move all employees to other department
- ✓ Delete → Success (soft delete)

---

## 7. PAYROLL REQUIREMENTS

### BR-PAYROLL-001: Salary Component Deletion Protection
**Requirement**: Cannot delete salary component that is used in any salary structure.

**Acceptance Criteria**:
- Check if component referenced in SalaryStructure.ComponentsJson
- Return error message indicating component is in use
- Allow deletion only if no references exist
- Soft delete (IsActive=false) instead of hard delete

**Test Scenarios**:
- ✓ Create salary component "Basic Salary"
- ✓ Create salary structure using "Basic Salary" component
- ✓ Attempt delete "Basic Salary" → Error "Cannot delete salary component that is used in salary structures"
- ✓ Remove component from all structures
- ✓ Delete component → Success (soft delete)

### BR-PAYROLL-002: One Payroll Run Per Month Per Tenant
**Requirement**: Only one active payroll run allowed per month/year combination per tenant.

**Acceptance Criteria**:
- Check for existing run with same month/year/tenant
- Reject creation if active run exists (status != Rejected)
- Allow creation after previous run is Published or Rejected
- Unique index on (tenant_id, month, year)

**Test Scenarios**:
- ✓ Create payroll run for January 2026 → Success
- ✓ Attempt create another run for January 2026 → Error "Payroll run already exists for this period"
- ✓ Process and publish January run
- ✓ Create run for February 2026 → Success (different month)
- ✓ Different tenant creates January 2026 run → Success (tenant isolation)

### BR-PAYROLL-003: Payroll Run Status Workflow
**Requirement**: Payroll run follows strict status progression.

**Acceptance Criteria**:
- Draft: Can edit, can delete
- Processing: System-only status during payslip generation
- Completed: Cannot edit, cannot delete, can re-process (amendment)
- Published: Immutable, cannot delete, payslips visible to employees
- Rejected: Terminal state, can create new run for same period

**Test Scenarios**:
- ✓ Create run → Status = Draft
- ✓ Delete draft run → Success
- ✓ Process run → Status changes Draft → Processing → Completed
- ✓ Attempt delete completed run → Error "Cannot delete processed payroll run"
- ✓ Publish run → Status = Published
- ✓ Attempt modify published run → Error

### BR-PAYROLL-004: Salary Structure Component Validation
**Requirement**: SalaryStructure.ComponentsJson must reference valid, active components.

**Acceptance Criteria**:
- All component IDs must exist in SalaryComponent table
- All referenced components must be active (IsActive = true)
- Component types must match usage (Earnings/Deductions separated)
- PERCENT components must have valid base reference

**Test Scenarios**:
- ✓ Create structure with non-existent component ID → Error "Invalid component"
- ✓ Create structure with inactive component → Error "Component is inactive"
- ✓ Create structure with all valid, active components → Success
- ✓ Deactivate component used in structure → Component stays in structure but warning on calculation

### BR-PAYROLL-005: Pro-Rata Salary Calculation
**Requirement**: Salary calculated proportional to working days and present days.

**Acceptance Criteria**:
- Monthly Gross = Sum of all earnings components
- Pro-Rated Gross = (Monthly Gross / Working Days) * Present Days
- Working Days = Total days - Weekends - Holidays
- Present Days = Attendance records + Approved paid leave
- Net Pay = Gross Earnings - Total Deductions (minimum 0)

**Test Scenarios**:
- ✓ Employee monthly salary: 10,000 AED
- ✓ Working days in month: 22
- ✓ Present days: 20 (2 days unpaid leave)
- ✓ Gross calculation: (10,000 / 22) * 20 = 9,090.91 AED
- ✓ Deductions: 500 AED (tax)
- ✓ Net Pay: 9,090.91 - 500 = 8,590.91 AED

### BR-PAYROLL-006: Payslip Immutability
**Requirement**: Payslips cannot be modified once generated.

**Acceptance Criteria**:
- No UPDATE endpoint for payslips
- Corrections handled via payroll run re-processing
- Amendment creates new payroll run with adjustments
- Original payslip remains in audit trail

**Test Scenarios**:
- ✓ Generate payslip → Status saved to database
- ✓ Attempt direct update to payslip → No API endpoint exists
- ✓ Error in calculation → Re-process entire run (creates new payslips)
- ✓ Both original and corrected payslips visible in history

### BR-PAYROLL-007: Code Uniqueness Per Tenant
**Requirement**: Salary component codes must be unique within tenant.

**Acceptance Criteria**:
- Code field is required, 2-50 characters
- Code format: Uppercase letters, numbers, hyphens, underscores only
- Unique index on (tenant_id, code)
- Clear error message on duplicate code

**Test Scenarios**:
- ✓ Tenant A creates component with code "BASIC" → Success
- ✓ Tenant A creates another component with code "BASIC" → Error "Salary component with code 'BASIC' already exists"
- ✓ Tenant B creates component with code "BASIC" → Success (different tenant)
- ✓ Update component code to existing code → Error

---

## 8. DATA VALIDATION REQUIREMENTS

### BR-VAL-001: Email Uniqueness
**Requirement**: Email addresses must be unique within tenant.

**Test Scenarios**:
- ✓ Create user with email@test.com → Success
- ✓ Create another user with email@test.com → Error "Email already exists"

### BR-VAL-002: Employee Code Uniqueness
**Requirement**: Employee codes must be unique within tenant.

**Test Scenarios**:
- ✓ Create employee with code "EMP001" → Success
- ✓ Create another employee "EMP001" → Error "Code already exists"
- ✓ Different tenant can use "EMP001" → Success (tenant isolation)

### BR-VAL-003: Date Range Validation
**Requirement**: End date must be >= start date.

**Test Scenarios**:
- ✓ Leave request: StartDate=Jan 15, EndDate=Jan 10 → Error
- ✓ Leave request: StartDate=Jan 15, EndDate=Jan 15 → Success (same day)

---

## 9. SECURITY REQUIREMENTS

### BR-SEC-001: Password Hashing
**Requirement**: Passwords MUST be hashed using BCrypt (never stored plain text).

**Test Scenarios**:
- ✓ Create user with password "Test@123"
- ✓ Query database directly → password_hash field is BCrypt hash
- ✓ Login with "Test@123" → Success (hash verification)
- ✓ Login with wrong password → Error

### BR-SEC-002: SQL Injection Prevention
**Requirement**: All database queries use parameterized queries.

**Test Scenarios**:
- ✓ Search employee with name "'; DROP TABLE employees; --"
- ✓ No SQL injection occurs
- ✓ Search returns empty or safe error

---

## 10. PERFORMANCE REQUIREMENTS

### BR-PERF-001: Query Optimization
**Requirement**: List endpoints must support pagination.

**Acceptance Criteria**:
- Default page size: 50 items
- Max page size: 100 items
- Include total count in response

**Test Scenarios**:
- ✓ Get employees without pagination → Returns first 50
- ✓ Get employees with pageSize=10 → Returns 10 items
- ✓ Response includes total count

---

## Test Priority Matrix

| Priority | Category | Business Impact |
|----------|----------|-----------------|
| P0 (Critical) | Multi-tenancy, Authentication, Data Isolation | System security breach |
| P1 (High) | Leave balance, Attendance, Payroll accuracy | Financial/Legal risk |
| P2 (Medium) | Geofencing, Roster management, Notifications | User experience |
| P3 (Low) | Reports, Analytics, Audit logs | Nice to have |

---

## Test Coverage Goals

- **Unit Tests**: 80% code coverage (business logic only)
- **Integration Tests**: 100% of critical API endpoints (P0, P1)
- **Contract Tests**: 100% API schema validation
- **E2E Tests**: Core user journeys (10 critical paths)

---

**Next Steps**:
1. Implement Unit Tests based on these requirements
2. Implement Integration Tests (HTTP-based)
3. Implement Contract Tests (OpenAPI validation)
4. Setup CI/CD pipeline to run tests automatically
