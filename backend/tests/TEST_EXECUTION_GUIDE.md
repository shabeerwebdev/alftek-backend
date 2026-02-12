# Test Execution Guide - AlfTekPro HRMS

## 🎯 Test Suite Overview

### What We Built

A comprehensive **requirements-driven test suite** that validates business rules, NOT implementation details.

```
✅ Business Requirements Document (40+ requirements)
✅ Unit Tests (4 service test suites, 35+ test cases)
✅ Integration Tests (Multi-tenancy isolation - CRITICAL)
✅ Test Infrastructure (xUnit + FluentAssertions + InMemory DB)
```

---

## 📊 Test Coverage Summary

| Module | Business Rules | Unit Tests | Integration Tests | Status |
|--------|---------------|------------|-------------------|---------|
| **Leave Management** | BR-LEAVE-001 to BR-LEAVE-004 | ✅ 8 tests | ⏳ Pending | Complete |
| **Attendance** | BR-ATT-001 to BR-ATT-005 | ✅ 12 tests | ⏳ Pending | Complete |
| **Department** | BR-DEPT-001, BR-DEPT-002 | ✅ 8 tests | ⏳ Pending | Complete |
| **Employee Roster** | BR-ROSTER-001 to BR-ROSTER-003 | ✅ 10 tests | ⏳ Pending | Complete |
| **Multi-Tenancy** | BR-MT-001, BR-MT-002 | N/A | ✅ 3 tests | Complete |
| **Authentication** | BR-AUTH-001 to BR-AUTH-003 | ⏳ Pending | ⏳ Pending | Pending |
| **Leave Balance** | Balance calculations | ⏳ Pending | ⏳ Pending | Pending |

**Total**: 41 test cases created validating 15+ critical business requirements

---

## 🚀 Quick Start - Run Tests

### 1. Prerequisites

```bash
# Ensure .NET 8 SDK installed
dotnet --version
# Should show: 8.0.x

# Navigate to backend directory
cd c:\Users\Admin\Documents\alftekpro\backend
```

### 2. Restore & Build

```bash
# Restore all packages
dotnet restore

# Build entire solution
dotnet build
```

### 3. Run ALL Tests

```bash
# Run all tests (unit + integration)
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with logger for better readability
dotnet test --logger "console;verbosity=detailed"
```

Expected output:
```
Test run for AlfTekPro.UnitTests.dll (.NET 8.0)
Test run for AlfTekPro.IntegrationTests.dll (.NET 8.0)

Passed!  - Failed:     0, Passed:    41, Skipped:     0, Total:    41
```

### 4. Run Specific Test Project

```bash
# Unit tests only (FAST - in-memory)
dotnet test tests/AlfTekPro.UnitTests/AlfTekPro.UnitTests.csproj

# Integration tests only (SLOWER - HTTP + DB)
dotnet test tests/AlfTekPro.IntegrationTests/AlfTekPro.IntegrationTests.csproj
```

### 5. Run Specific Test Class

```bash
# Run only Leave Request tests
dotnet test --filter "FullyQualifiedName~LeaveRequestServiceTests"

# Run only Multi-tenancy tests (CRITICAL)
dotnet test --filter "FullyQualifiedName~MultiTenancyTests"

# Run only Attendance tests
dotnet test --filter "FullyQualifiedName~AttendanceLogServiceTests"
```

### 6. Run Single Test Method

```bash
# Run specific test case
dotnet test --filter "FullyQualifiedName~ClockIn_WhenAlreadyClockedInToday_ShouldFail"
```

---

## 🔍 Test Details by Module

### 1. Leave Request Service Tests
**File**: `tests/AlfTekPro.UnitTests/Services/LeaveRequestServiceTests.cs`

**Business Rules Validated**:
- ✅ BR-LEAVE-001: Insufficient balance prevention
- ✅ BR-LEAVE-002: Overlapping leave prevention
- ✅ BR-LEAVE-003: Balance deduction on approval only
- ✅ BR-LEAVE-004: Auto-approval for no-approval-required types

**Test Cases** (8 total):
```
✓ CreateLeaveRequest_WhenInsufficientBalance_ShouldFail
✓ CreateLeaveRequest_WhenSufficientBalance_ShouldSucceed
✓ CreateLeaveRequest_WhenOverlappingWithExisting_ShouldFail
✓ CreateLeaveRequest_WhenNoOverlap_ShouldSucceed
✓ CreateLeaveRequest_WhenOverlappingButPreviousRejected_ShouldSucceed
✓ ProcessLeaveRequest_WhenApproved_ShouldDeductBalance
✓ ProcessLeaveRequest_WhenRejected_ShouldNotDeductBalance
✓ CreateLeaveRequest_WhenNoApprovalRequired_ShouldAutoApprove
```

**Run command**:
```bash
dotnet test --filter "FullyQualifiedName~LeaveRequestServiceTests"
```

---

### 2. Attendance Log Service Tests
**File**: `tests/AlfTekPro.UnitTests/Services/AttendanceLogServiceTests.cs`

**Business Rules Validated**:
- ✅ BR-ATT-001: Single clock-in per day
- ✅ BR-ATT-002: Geofencing validation
- ✅ BR-ATT-003: Late detection based on shift
- ✅ BR-ATT-004: Clock-out validation
- ✅ BR-ATT-005: Attendance regularization

**Test Cases** (12 total):
```
✓ ClockIn_WhenFirstTimeToday_ShouldSucceed
✓ ClockIn_WhenAlreadyClockedInToday_ShouldFail
✓ ClockIn_OnDifferentDay_ShouldSucceed
✓ ClockIn_WhenWithinGeofence_ShouldSucceed
✓ ClockIn_WhenOutsideGeofence_ShouldFail
✓ ClockIn_WhenNoLocationConfigured_ShouldFailGracefully
✓ ClockIn_WhenOnTime_ShouldNotMarkLate
✓ ClockOut_WhenNoClockIn_ShouldFail
✓ ClockOut_WhenAlreadyClockedOut_ShouldFail
✓ ClockOut_WhenValidClockIn_ShouldCalculateTotalHours
✓ RegularizeAttendance_WhenLateAttendance_ShouldClearLateFlag
✓ RegularizeAttendance_WhenAlreadyRegularized_ShouldFail
```

**Run command**:
```bash
dotnet test --filter "FullyQualifiedName~AttendanceLogServiceTests"
```

---

### 3. Department Service Tests
**File**: `tests/AlfTekPro.UnitTests/Services/DepartmentServiceTests.cs`

**Business Rules Validated**:
- ✅ BR-DEPT-001: Circular reference prevention
- ✅ BR-DEPT-002: Department deletion with employees

**Test Cases** (8 total):
```
✓ CreateDepartment_WhenValidHierarchy_ShouldSucceed
✓ UpdateDepartment_WhenCreatingCircularReference_ShouldFail
✓ UpdateDepartment_WhenMakingChildOwnParent_ShouldFail
✓ UpdateDepartment_WhenBreakingCircularChain_ShouldSucceed
✓ DeleteDepartment_WhenHasEmployees_ShouldFail
✓ DeleteDepartment_WhenNoEmployees_ShouldSoftDelete
✓ DeleteDepartment_WhenHasChildDepartments_ShouldFail
✓ GetDepartmentHierarchy_ShouldReturnNestedStructure
```

**Run command**:
```bash
dotnet test --filter "FullyQualifiedName~DepartmentServiceTests"
```

---

### 4. Employee Roster Service Tests
**File**: `tests/AlfTekPro.UnitTests/Services/EmployeeRosterServiceTests.cs`

**Business Rules Validated**:
- ✅ BR-ROSTER-001: No duplicate roster on same date
- ✅ BR-ROSTER-002: Current roster calculation
- ✅ BR-ROSTER-003: Inactive shift prevention

**Test Cases** (10 total):
```
✓ CreateRoster_WhenFirstTimeForDate_ShouldSucceed
✓ CreateRoster_WhenDuplicateEffectiveDate_ShouldFail
✓ CreateRoster_WhenDifferentDate_ShouldSucceed
✓ GetCurrentRoster_ShouldReturnMostRecentPastRoster
✓ GetCurrentRoster_WhenNoActiveRoster_ShouldReturnNull
✓ CreateRoster_WhenShiftInactive_ShouldFail
✓ CreateRoster_WhenShiftActive_ShouldSucceed
✓ CreateRoster_WhenEmployeeNotFound_ShouldFail
✓ CreateRoster_WhenShiftNotFound_ShouldFail
✓ UpdateRoster_WhenChangingToInactiveShift_ShouldFail
```

**Run command**:
```bash
dotnet test --filter "FullyQualifiedName~EmployeeRosterServiceTests"
```

---

### 5. Multi-Tenancy Integration Tests ⚠️ CRITICAL
**File**: `tests/AlfTekPro.IntegrationTests/MultiTenancyTests.cs`

**Priority**: **P0 - CRITICAL** (Security breach if fails)

**Business Rules Validated**:
- ✅ BR-MT-001: Complete data isolation
- ✅ BR-MT-002: Automatic tenant_id injection

**Test Cases** (3 total):
```
✓ GetEmployees_WhenTenantA_ShouldNotSeeTenantBData
✓ GetEmployeeById_WhenDifferentTenant_ShouldReturn404
✓ CreateEmployee_ShouldAutoAssignCorrectTenantId
```

**Run command**:
```bash
dotnet test --filter "FullyQualifiedName~MultiTenancyTests"
```

**⚠️ IMPORTANT**: These tests verify tenant isolation. **DO NOT SKIP**.

---

## ✅ Test Success Criteria

### What PASS Means

When tests pass, it means:

1. **Business Requirements Met**: The code implements the business rules correctly
2. **No Regression**: Changes haven't broken existing functionality
3. **Security Validated**: Multi-tenancy isolation is working (P0)
4. **Data Integrity**: Leave balances, attendance logic, etc. work correctly

### What FAIL Means

When tests fail, it means:

1. **Business Rule Violated**: Code doesn't meet requirements
2. **Bug Introduced**: Recent changes broke existing functionality
3. **Security Risk**: Potential data leak between tenants
4. **Fix the CODE**: Tests are correct (based on requirements), code is wrong

---

## 🐛 Troubleshooting

### Tests Failing?

#### 1. Check Business Requirements First
```bash
# Open requirements document
code tests/BUSINESS_REQUIREMENTS.md

# Verify the failing test matches the business rule
# Example: BR-LEAVE-001 says "Cannot approve if insufficient balance"
# Test should fail when trying to approve with insufficient balance
```

#### 2. Review Test Output
```bash
# Run with verbose logging
dotnet test --verbosity detailed

# Look for the assertion that failed
# Example:
#   Expected: 5
#   Actual: 7
#   Message: "Available balance should be 5, but was 7"
```

#### 3. Verify Test Data
```bash
# Each test uses in-memory database
# Check SeedTestData() method in test class
# Verify test data matches business scenario
```

#### 4. Check for Test Isolation Issues
```csharp
// Each test should clean up
public void Dispose()
{
    _context.Database.EnsureDeleted();  // Clean up in-memory DB
    _context.Dispose();
}
```

### Integration Tests Timing Out?

```bash
# Increase timeout in test settings
# Create test.runsettings:

<RunSettings>
  <RunConfiguration>
    <TestSessionTimeout>600000</TestSessionTimeout>
  </RunConfiguration>
</RunSettings>

# Run with settings
dotnet test --settings test.runsettings
```

### Can't Find Test DLL?

```bash
# Rebuild solution
dotnet clean
dotnet build
dotnet test
```

---

## 📈 Next Steps - Expanding Test Coverage

### Immediate Priority

1. **Run Existing Tests** ✅
   ```bash
   dotnet test
   ```

2. **Fix Any Failures** (if any)
   - Review business requirements
   - Fix CODE to match requirements
   - Re-run tests

3. **Add Missing Tests** (Recommended Order):
   - [ ] Authentication Service tests (BR-AUTH-001 to BR-AUTH-003)
   - [ ] Leave Balance Service tests (balance calculations, initialization)
   - [ ] Integration tests for Leave Management workflow
   - [ ] Integration tests for Attendance workflow
   - [ ] Performance tests (optional)

### Test Template for New Features

When adding new features:

```csharp
/// <summary>
/// Tests for [Feature Name]
/// Reference: BR-XXX-YYY (from BUSINESS_REQUIREMENTS.md)
/// </summary>
public class FeatureServiceTests : IDisposable
{
    #region BR-XXX-YYY: Business Rule Description

    [Fact]
    public async Task Method_WhenCondition_ShouldExpectedBehavior()
    {
        // Arrange - BR-XXX-YYY: State the business rule
        // ... setup test data

        // Act
        var result = await _service.MethodAsync(...);

        // Assert - Validate business outcome
        result.Should().NotBeNull();
        result.SomeProperty.Should().Be(expectedValue);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

---

## 🎓 Key Takeaways

### Testing Philosophy

1. **Tests Validate Requirements, Not Code**
   - If test fails, fix the CODE
   - Tests are "executable business requirements"

2. **Business-First Approach**
   - Write requirements first (BUSINESS_REQUIREMENTS.md)
   - Write tests second (validate requirements)
   - Write code third (make tests pass)

3. **Clear Test Names**
   - `Method_WhenCondition_ShouldBehavior`
   - Example: `ClockIn_WhenOutsideGeofence_ShouldFail`

4. **Reference Business Rules**
   - Every test references BR-XXX-YYY
   - Traceability from requirement to test to code

---

## 📞 Support

If tests fail and you're unsure why:

1. Check `BUSINESS_REQUIREMENTS.md` for the BR-XXX-YYY reference
2. Review test output with `--verbosity detailed`
3. Verify test data matches business scenario
4. Check if code implements the business rule correctly

**Remember**: Tests are the specification. Code must match tests, not vice versa.

---

## ✅ Checklist Before Deployment

- [ ] All unit tests pass (`dotnet test tests/AlfTekPro.UnitTests`)
- [ ] All integration tests pass (`dotnet test tests/AlfTekPro.IntegrationTests`)
- [ ] **CRITICAL**: Multi-tenancy tests pass (security requirement)
- [ ] Code coverage > 70% for business logic
- [ ] All P0 and P1 tests pass
- [ ] No skipped tests without documented reason

---

**Ready to run?**

```bash
cd c:\Users\Admin\Documents\alftekpro\backend
dotnet test
```

🎯 **Expected**: 41 tests passed, 0 failed
