# AlfTekPro HRMS - Test Suite

This directory contains comprehensive tests based on **BUSINESS REQUIREMENTS**, not code implementation.

## Philosophy

**Tests validate WHAT the system should do (business requirements), NOT HOW it does it (implementation).**

If a test fails, the CODE is wrong, not the test. Tests are written from the perspective of:
- Business analysts defining requirements
- Product managers defining acceptance criteria
- End users expecting specific behavior

## Test Structure

```
tests/
├── BUSINESS_REQUIREMENTS.md          # Source of truth for what system MUST do
├── AlfTekPro.UnitTests/              # Fast, isolated business logic tests
│   ├── Services/                     # Service layer tests
│   └── Validators/                   # Validation logic tests
├── AlfTekPro.IntegrationTests/       # HTTP API tests
│   ├── Controllers/                  # API endpoint tests
│   └── Fixtures/                     # Test setup/teardown
└── README.md                          # This file
```

## Running Tests

### Prerequisites
```bash
# Restore dependencies
cd backend
dotnet restore

# Build solution
dotnet build
```

### Run All Tests
```bash
# Run all tests in solution
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=html
```

### Run Specific Test Projects
```bash
# Unit tests only (fast)
dotnet test tests/AlfTekPro.UnitTests/AlfTekPro.UnitTests.csproj

# Integration tests only (slower)
dotnet test tests/AlfTekPro.IntegrationTests/AlfTekPro.IntegrationTests.csproj
```

### Run Specific Test Class
```bash
# Run only LeaveRequestServiceTests
dotnet test --filter "FullyQualifiedName~LeaveRequestServiceTests"

# Run only tests matching pattern
dotnet test --filter "Name~InsufficientBalance"
```

### Run Tests by Category
```bash
# Critical tests only (P0)
dotnet test --filter "Category=Critical"

# Business logic tests
dotnet test --filter "Category=BusinessLogic"
```

## Test Categories

Tests are organized by business priority:

| Category | Description | Example |
|----------|-------------|---------|
| `Critical` | P0 - System security, data isolation | Multi-tenancy, Authentication |
| `High` | P1 - Financial/Legal risk | Leave balance, Payroll accuracy |
| `Medium` | P2 - User experience | Geofencing, Notifications |
| `Low` | P3 - Nice to have | Reports, Analytics |

## Writing New Tests

### 1. Start with Business Requirement

```csharp
// ❌ BAD: Test validates implementation
[Fact]
public void Service_CallsRepository_WithCorrectParameters()
{
    // This just confirms what code does, not what it SHOULD do
}

// ✅ GOOD: Test validates business rule
[Fact]
public void CreateLeaveRequest_WhenInsufficientBalance_ShouldFail()
{
    // Arrange - BR-LEAVE-001: Business rule from requirements doc
    // Employee has 5 days remaining, requests 7 days

    // Act & Assert
    // MUST fail with clear error message
}
```

### 2. Reference Business Requirements

```csharp
/// <summary>
/// Tests for Leave Request business logic
/// Reference: BR-LEAVE-001, BR-LEAVE-002, BR-LEAVE-003
/// See: tests/BUSINESS_REQUIREMENTS.md
/// </summary>
public class LeaveRequestServiceTests
{
    #region BR-LEAVE-001: Insufficient Balance Prevention

    [Fact]
    public async Task CreateLeaveRequest_WhenInsufficientBalance_ShouldFail()
    {
        // Test implementation
    }

    #endregion
}
```

### 3. Use Descriptive Test Names

Follow pattern: `MethodName_WhenCondition_ShouldExpectedBehavior`

```csharp
CreateLeaveRequest_WhenInsufficientBalance_ShouldFail
ClockIn_WhenOutsideGeofence_ShouldRejectWithLocationError
ProcessApproval_WhenApproved_ShouldDeductBalanceAtomically
```

### 4. Assert Business Outcomes, Not Implementation Details

```csharp
// ❌ BAD: Testing implementation
result.DbContext.SaveChangesAsync.Should().HaveBeenCalled();

// ✅ GOOD: Testing business outcome
updatedBalance.Used.Should().Be(3); // Balance actually deducted
exception.Message.Should().Contain("Insufficient balance");
```

## Test Data Strategy

### In-Memory Database for Unit Tests
```csharp
var options = new DbContextOptionsBuilder<HrmsDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

### Test Database for Integration Tests
```csharp
// Use actual PostgreSQL test database
// or Docker container spun up for tests
var connectionString = "Host=localhost;Database=hrms_test;..."
```

## Continuous Integration

Tests run automatically on:
- Every commit to main branch
- Every pull request
- Nightly builds (full test suite + performance tests)

### CI Pipeline
```yaml
# .github/workflows/test.yml
steps:
  - name: Run Unit Tests
    run: dotnet test tests/AlfTekPro.UnitTests

  - name: Run Integration Tests
    run: dotnet test tests/AlfTekPro.IntegrationTests

  - name: Code Coverage
    run: dotnet test /p:CollectCoverage=true
```

## Test Coverage Goals

| Test Type | Coverage Target | Current |
|-----------|----------------|---------|
| Unit Tests | 80% of business logic | TBD |
| Integration Tests | 100% of critical endpoints (P0, P1) | TBD |
| Contract Tests | 100% API schema validation | TBD |

## Common Test Scenarios

### Multi-Tenancy Isolation
```csharp
[Fact]
public async Task GetEmployees_WhenTenantA_ShouldNotSeeTenantBData()
{
    // BR-MT-001: Complete data isolation
    // Create employees in both tenants
    // Login as Tenant A
    // MUST NOT see Tenant B employees
}
```

### Authentication & Authorization
```csharp
[Fact]
public async Task CreateDepartment_WhenEmployee_ShouldReturn403Forbidden()
{
    // BR-AUTH-002: Role-based access control
    // Login as Employee (not Manager/Admin)
    // Attempt to create department
    // MUST return 403 Forbidden
}
```

### Leave Balance Validation
```csharp
[Fact]
public async Task ApproveLeave_WhenInsufficientBalance_ShouldFailAndNotDeduct()
{
    // BR-LEAVE-001 + BR-LEAVE-003
    // Employee has 5 days, manager approves 7 days
    // MUST fail
    // Balance MUST NOT be deducted
}
```

### Geofencing
```csharp
[Fact]
public async Task ClockIn_WhenOutsideRadius_ShouldRejectWithDistance()
{
    // BR-ATT-002: Geofencing validation
    // Office at (25.2048, 55.2708), radius 100m
    // Employee at (25.3000, 55.3000) - far away
    // MUST reject with error showing allowed location
}
```

## Troubleshooting

### Tests Failing Unexpectedly

1. **Check Business Requirements First**
   - Does the test match the business requirement?
   - Has the business requirement changed?
   - Is the code implementing the requirement correctly?

2. **Verify Test Data**
   - Is test data properly seeded?
   - Are IDs unique (use Guid.NewGuid())?
   - Is tenant context set correctly?

3. **Check In-Memory DB State**
   - Each test should use fresh database
   - Dispose context properly
   - SaveChanges called where needed

### Integration Tests Timing Out

```bash
# Increase test timeout
dotnet test --settings test.runsettings

# test.runsettings:
<RunSettings>
  <TestRunParameters>
    <Parameter name="webAppUrl" value="http://localhost:5000" />
  </TestRunParameters>
  <RunConfiguration>
    <TestSessionTimeout>300000</TestSessionTimeout>
  </RunConfiguration>
</RunSettings>
```

## Resources

- Business Requirements: `tests/BUSINESS_REQUIREMENTS.md`
- xUnit Documentation: https://xunit.net/
- FluentAssertions: https://fluentassertions.com/
- Test Coverage: https://github.com/coverlet-coverage/coverlet

## Contributing

When adding new features:

1. **Update Business Requirements FIRST**
   - Add new BR-XXX-YYY entries
   - Define acceptance criteria
   - Get product owner approval

2. **Write Tests SECOND**
   - Write tests based on BR acceptance criteria
   - Tests should FAIL initially (TDD)

3. **Implement Code THIRD**
   - Write code to make tests pass
   - Tests validate business requirements are met

4. **Code Review**
   - Review tests first (do they match requirements?)
   - Then review implementation

Remember: **Tests are business requirements in executable form.**
