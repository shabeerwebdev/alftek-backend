# High-Level System Design: Multi-Tenant HRMS

## 1. Architecture Overview

### 1.1 System Architecture Pattern
**Modular Monolith with Multi-Tenant Isolation**

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend Layer                           │
│  Next.js 14+ (App Router) + Ant Design + TanStack Query         │
└────────────────┬────────────────────────────────────────────────┘
                 │ HTTPS/REST
┌────────────────▼────────────────────────────────────────────────┐
│                      API Gateway Layer                           │
│        .NET Core 8+ Web API + JWT Auth + Rate Limiting          │
└────────────────┬────────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────────┐
│                    Application Layer                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │
│  │ Platform │ │ Core HR  │ │  Leave   │ │ Payroll  │          │
│  │  Module  │ │  Module  │ │  Module  │ │  Module  │          │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                        │
│  │Attendance│ │  Assets  │ │  Schema  │                        │
│  │  Module  │ │  Module  │ │  Engine  │                        │
│  └──────────┘ └──────────┘ └──────────┘                        │
└────────────────┬────────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────────┐
│                    Infrastructure Layer                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐           │
│  │  PostgreSQL  │ │     Redis    │ │  Azure Blob  │           │
│  │ (Multi-Tenant│ │    (Cache)   │ │  (Storage)   │           │
│  │  Database)   │ │              │ │              │           │
│  └──────────────┘ └──────────────┘ └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Technology Stack

### 2.1 Frontend Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | Next.js 14+ (App Router) | SSR, React Server Components, API Routes |
| **UI Library** | Ant Design 5.x | Enterprise-grade components |
| **State Management** | TanStack Query (React Query) | Server state, caching, optimistic updates |
| **Form Handling** | React Hook Form + Zod | Dynamic form validation |
| **HTTP Client** | Axios with interceptors | API communication, token refresh |
| **Date/Time** | dayjs | Timezone-aware date handling |
| **Charts** | Recharts / Ant Design Charts | Analytics dashboards |
| **Internationalization** | next-intl | Multi-language support |

### 2.2 Backend Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | .NET Core 8+ Web API | RESTful API, minimal APIs |
| **ORM** | Entity Framework Core 8+ | Database access, migrations |
| **Database** | PostgreSQL 15+ | Multi-tenant data store |
| **Cache** | Redis (StackExchange.Redis) | Session, permissions, schema cache |
| **Authentication** | JWT (System.IdentityModel.Tokens) | Stateless auth |
| **Authorization** | Custom RBAC Middleware | Permission-based access control |
| **File Storage** | Azure Blob Storage / S3 | Payslips, documents, assets |
| **PDF Generation** | QuestPDF | Payslips, reports |
| **Background Jobs** | Hangfire | Scheduled payroll, notifications |
| **Logging** | Serilog + Seq | Structured logging, audit trails |
| **Validation** | FluentValidation | Input validation |
| **Mapping** | AutoMapper | DTO <-> Entity mapping |

---

## 3. Multi-Tenancy Strategy

### 3.1 Database Isolation: Row-Level Tenancy (Shared Database, Shared Schema)

**Why This Approach?**
- Cost-effective for MVP (single database instance)
- Easy to query across tenants for platform analytics
- Simplified backups and maintenance

**Implementation:**
```csharp
// Every tenant-scoped table has TenantId
public abstract class TenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Global Query Filter (EF Core)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Employee>().HasQueryFilter(e => e.TenantId == _currentTenantId);
    modelBuilder.Entity<LeaveRequest>().HasQueryFilter(l => l.TenantId == _currentTenantId);
    // Applied to all tenant-scoped entities
}
```

**Tenant Resolution Hierarchy:**
1. **JWT Token Claim**: `tenantId` embedded in token
2. **Middleware**: Extracts tenantId and sets in `ITenantContext`
3. **EF Core Interceptor**: Automatically injects TenantId on SaveChanges

```csharp
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantId = context.User.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
        if (Guid.TryParse(tenantId, out var id))
        {
            tenantContext.SetTenantId(id);
        }
        await _next(context);
    }
}
```

---

## 4. Module Design

### 4.1 Platform Module (Global Services)

**Responsibilities:**
- Tenant provisioning and management
- Subscription billing (future)
- Global master data (Countries, Currencies, Timezones)
- Platform-wide audit logs

**Key Entities:**
```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; }
    public string SubDomain { get; set; } // e.g., "acme" -> acme.hrms.com
    public Guid RegionId { get; set; } // Links to Region (UAE, India, US)
    public DateTime SubscriptionStart { get; set; }
    public DateTime? SubscriptionEnd { get; set; }
    public TenantStatus Status { get; set; } // Active, Suspended, Trial
}

public class Region
{
    public Guid Id { get; set; }
    public string Code { get; set; } // "UAE", "IND", "USA"
    public string CurrencyCode { get; set; } // "AED", "INR", "USD"
    public string DateFormat { get; set; } // "DD/MM/YYYY"
    public string Timezone { get; set; } // "Asia/Dubai"
}
```

**API Endpoints:**
```
POST   /platform/tenants              [SA]  Create new tenant
GET    /platform/tenants              [SA]  List all tenants
PUT    /platform/tenants/{id}/status  [SA]  Activate/Suspend
GET    /platform/regions              [SA]  List supported regions
POST   /platform/regions              [SA]  Add new region
```

---

### 4.2 Core HR Module

**Responsibilities:**
- Organization structure (Departments, Designations)
- Employee lifecycle (Onboarding, Profile, Offboarding)
- Dynamic employee profiles (via Schema Engine)

**Key Entities:**
```csharp
public class Employee : TenantEntity
{
    public string EmployeeCode { get; set; } // ACM-001
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid? ManagerId { get; set; } // Self-referencing for org chart
    public DateTime JoiningDate { get; set; }
    public EmploymentStatus Status { get; set; } // Active, OnLeave, Terminated
    public virtual Department Department { get; set; }
    public virtual Employee Manager { get; set; }
}

public class Department : TenantEntity
{
    public string Name { get; set; }
    public Guid? ParentDepartmentId { get; set; } // For nested departments
}

public class Designation : TenantEntity
{
    public string Title { get; set; } // "Software Engineer", "Manager"
    public int Level { get; set; } // For hierarchy
}
```

**Dynamic Profile Data:**
```csharp
public class EmployeeProfileData : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public string FieldKey { get; set; } // "emirates_id", "blood_group"
    public string Value { get; set; } // JSON serialized value
    public Guid SchemaFieldId { get; set; } // Links to schema definition
}
```

**API Endpoints:**
```
POST   /employees                     [TA]     Create employee
GET    /employees                     [TA,MGR] List employees (filtered by permission)
GET    /employees/{id}                [TA,MGR,EMP] Get profile (own or managed)
PUT    /employees/{id}                [TA]     Update employee
GET    /employees/{id}/org-chart      [TA,MGR] Get reporting hierarchy
POST   /employees/{id}/terminate      [TA]     Offboard employee

GET    /departments                   [TA,MGR] List departments
POST   /departments                   [TA]     Create department
GET    /designations                  [TA]     List designations
```

---

### 4.3 Schema Engine Module

**Responsibilities:**
- Define dynamic form fields per region/tenant
- Store field configurations (type, validation, visibility)
- Serve schema to frontend for dynamic form rendering

**Key Entities:**
```csharp
public class SchemaDefinition : TenantEntity
{
    public string Module { get; set; } // "Employee", "Leave", "Payroll"
    public Guid RegionId { get; set; } // Which region this schema applies to
    public bool IsActive { get; set; }
}

public class SchemaField
{
    public Guid Id { get; set; }
    public Guid SchemaDefinitionId { get; set; }
    public string FieldKey { get; set; } // "emirates_id"
    public string Label { get; set; } // "Emirates ID"
    public string FieldType { get; set; } // "text", "number", "date", "dropdown"
    public bool IsRequired { get; set; }
    public string ValidationRules { get; set; } // JSON: { "pattern": "^[0-9]{15}$" }
    public int DisplayOrder { get; set; }
    public string Section { get; set; } // "Personal Info", "Documents"
    public string OptionsSource { get; set; } // For dropdowns: "api:/bloodgroups" or JSON
}
```

**API Endpoints:**
```
GET    /schema/{module}?region={code}  [All] Get form schema
POST   /schema                         [TA]  Create custom schema
PUT    /schema/{id}/fields             [TA]  Add/Update fields
```

**Frontend Consumption:**
```typescript
// Next.js component
const { data: schema } = useQuery({
  queryKey: ['schema', 'employee', tenantRegion],
  queryFn: () => api.get(`/schema/employee?region=${tenantRegion}`)
});

return (
  <Form>
    {schema.fields.map(field => (
      <DynamicField key={field.fieldKey} config={field} />
    ))}
  </Form>
);
```

---

### 4.4 Leave Module

**Responsibilities:**
- Define leave types and policies
- Automatic accrual (monthly/yearly)
- Application workflow (Apply -> Approve -> Deduct)
- Balance tracking and carry-forward

**Key Entities:**
```csharp
public class LeaveType : TenantEntity
{
    public string Name { get; set; } // "Annual Leave", "Sick Leave"
    public string Code { get; set; } // "AL", "SL"
    public decimal MaxDaysPerYear { get; set; }
    public bool IsCarryForward { get; set; }
    public int MaxCarryForwardDays { get; set; }
    public bool RequiresApproval { get; set; }
}

public class LeavePolicy : TenantEntity
{
    public Guid LeaveTypeId { get; set; }
    public AccrualType AccrualType { get; set; } // Monthly, Yearly, JoiningDate
    public decimal AccrualRate { get; set; } // e.g., 2.5 days/month
    public int MinServiceMonths { get; set; } // Eligible after X months
}

public class LeaveBalance : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal Accrued { get; set; }
    public decimal Used { get; set; }
    public decimal Balance => Accrued - Used;
}

public class LeaveRequest : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysCount { get; set; } // Calculated (excludes weekends)
    public string Reason { get; set; }
    public LeaveStatus Status { get; set; } // Pending, Approved, Rejected, Cancelled
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApproverComments { get; set; }
}
```

**Business Logic (Service Layer):**
```csharp
public class LeaveService
{
    public async Task<Result> ApplyLeaveAsync(Guid employeeId, LeaveRequestDto dto)
    {
        // 1. Check balance
        var balance = await GetBalanceAsync(employeeId, dto.LeaveTypeId);
        if (balance < dto.DaysCount) return Result.Fail("Insufficient balance");

        // 2. Check overlapping requests
        var hasOverlap = await HasOverlappingRequestAsync(employeeId, dto.StartDate, dto.EndDate);
        if (hasOverlap) return Result.Fail("Overlapping leave exists");

        // 3. Create request
        var request = new LeaveRequest { ... };
        await _repo.AddAsync(request);

        // 4. Notify manager
        await _notificationService.NotifyManagerAsync(request);

        return Result.Success();
    }
}
```

**API Endpoints:**
```
GET    /leave/types                   [All]     List leave types
GET    /leave/balance/{employeeId}    [EMP,MGR] Get balances
POST   /leave/requests                [EMP]     Apply for leave
GET    /leave/requests                [EMP,MGR] List requests (own or team)
PUT    /leave/requests/{id}/approve   [MGR,TA]  Approve request
PUT    /leave/requests/{id}/reject    [MGR,TA]  Reject request
POST   /leave/policies                [TA]      Create leave policy
```

---

### 4.5 Attendance Module

**Responsibilities:**
- Clock-in/out tracking
- Geolocation validation (office radius check)
- Manual regularization workflow
- Overtime calculation

**Key Entities:**
```csharp
public class AttendanceRecord : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ClockIn { get; set; }
    public double? ClockInLatitude { get; set; }
    public double? ClockInLongitude { get; set; }
    public DateTime? ClockOut { get; set; }
    public double? ClockOutLatitude { get; set; }
    public double? ClockOutLongitude { get; set; }
    public AttendanceStatus Status { get; set; } // Present, Absent, HalfDay, Leave
    public decimal WorkedHours { get; set; }
    public bool IsRegularized { get; set; }
}

public class AttendanceRegularization : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public string Reason { get; set; }
    public DateTime ProposedClockIn { get; set; }
    public DateTime ProposedClockOut { get; set; }
    public RegularizationStatus Status { get; set; } // Pending, Approved, Rejected
    public Guid? ApprovedById { get; set; }
}

public class OfficeLocation : TenantEntity
{
    public string Name { get; set; } // "Head Office", "Branch A"
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } // Allowed check-in radius
}
```

**Geolocation Validation:**
```csharp
public class AttendanceService
{
    public async Task<Result> ClockInAsync(Guid employeeId, double lat, double lng)
    {
        var officeLocations = await _repo.GetOfficeLocationsAsync();
        var isWithinRange = officeLocations.Any(office =>
            CalculateDistance(lat, lng, office.Latitude, office.Longitude) <= office.RadiusMeters
        );

        if (!isWithinRange)
            return Result.Fail("You are not within office premises");

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = DateTime.UtcNow.Date,
            ClockIn = DateTime.UtcNow,
            ClockInLatitude = lat,
            ClockInLongitude = lng
        };

        await _repo.AddAsync(record);
        return Result.Success();
    }
}
```

**API Endpoints:**
```
POST   /attendance/clock-in           [EMP]     Clock in with geolocation
POST   /attendance/clock-out          [EMP]     Clock out
GET    /attendance/records            [EMP,MGR] List attendance records
POST   /attendance/regularize         [EMP]     Request regularization
PUT    /attendance/regularize/{id}    [MGR]     Approve regularization
GET    /attendance/summary            [MGR]     Team attendance report
```

---

### 4.6 Payroll Module

**Responsibilities:**
- Define salary structures (Basic, HRA, Allowances, Deductions)
- Monthly payroll run (integrates with Attendance & Leave)
- Generate payslips (PDF)
- Bank transfer file generation (NACHA, SEPA formats)

**Key Entities:**
```csharp
public class SalaryStructure : TenantEntity
{
    public string Name { get; set; } // "Software Engineer - L1"
    public Guid DesignationId { get; set; }
}

public class SalaryComponent : TenantEntity
{
    public Guid SalaryStructureId { get; set; }
    public string ComponentName { get; set; } // "Basic", "HRA", "Provident Fund"
    public ComponentType Type { get; set; } // Earning, Deduction
    public CalculationType CalculationType { get; set; } // Fixed, Percentage, Formula
    public string Value { get; set; } // "5000" or "40" (for 40% of Basic) or "Basic * 0.4"
    public bool IsTaxable { get; set; }
}

public class EmployeeSalary : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid SalaryStructureId { get; set; }
    public decimal CTC { get; set; } // Total annual package
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class PayrollCycle : TenantEntity
{
    public int Month { get; set; }
    public int Year { get; set; }
    public PayrollStatus Status { get; set; } // Draft, Processing, Completed, Published
    public DateTime? ProcessedAt { get; set; }
    public Guid ProcessedById { get; set; }
}

public class Payslip : TenantEntity
{
    public Guid PayrollCycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string PayslipPdfUrl { get; set; } // Azure Blob URL
    public List<PayslipComponent> Components { get; set; }
}

public class PayslipComponent
{
    public Guid Id { get; set; }
    public Guid PayslipId { get; set; }
    public string ComponentName { get; set; }
    public ComponentType Type { get; set; }
    public decimal Amount { get; set; }
}
```

**Payroll Calculation Logic:**
```csharp
public class PayrollService
{
    public async Task<Result> RunPayrollAsync(int month, int year)
    {
        var cycle = new PayrollCycle { Month = month, Year = year, Status = PayrollStatus.Processing };
        await _repo.AddAsync(cycle);

        var employees = await _employeeRepo.GetActiveEmployeesAsync();

        foreach (var emp in employees)
        {
            // 1. Get salary structure
            var salary = await _salaryRepo.GetCurrentSalaryAsync(emp.Id);

            // 2. Get attendance data
            var attendance = await _attendanceService.GetMonthlyAttendanceAsync(emp.Id, month, year);

            // 3. Calculate components
            var components = new List<PayslipComponent>();
            foreach (var component in salary.Structure.Components)
            {
                var amount = CalculateComponentAmount(component, salary.CTC, attendance);
                components.Add(new PayslipComponent { ComponentName = component.ComponentName, Amount = amount });
            }

            // 4. Generate payslip
            var payslip = new Payslip
            {
                EmployeeId = emp.Id,
                PresentDays = attendance.PresentDays,
                GrossEarnings = components.Where(c => c.Type == Earning).Sum(c => c.Amount),
                TotalDeductions = components.Where(c => c.Type == Deduction).Sum(c => c.Amount),
                Components = components
            };
            payslip.NetPay = payslip.GrossEarnings - payslip.TotalDeductions;

            // 5. Generate PDF
            payslip.PayslipPdfUrl = await _pdfService.GeneratePayslipAsync(payslip);

            await _repo.AddAsync(payslip);
        }

        cycle.Status = PayrollStatus.Completed;
        return Result.Success();
    }
}
```

**API Endpoints:**
```
POST   /payroll/structures            [PA]  Define salary structure
POST   /payroll/assign                [PA]  Assign salary to employee
POST   /payroll/cycles                [PA]  Run payroll for month
GET    /payroll/cycles                [PA]  List payroll cycles
POST   /payroll/cycles/{id}/publish   [PA]  Publish payslips to employees
GET    /payroll/payslips              [EMP] Get own payslips
GET    /payroll/payslips/{id}/pdf     [EMP] Download PDF
POST   /payroll/bank-file             [PA]  Generate bank transfer file
```

---

### 4.7 Assets Module

**Responsibilities:**
- Track company assets (Laptops, Phones, Furniture)
- Issue/Return workflow
- Condition tracking
- Asset lifecycle (Purchase -> Assign -> Return -> Dispose)

**Key Entities:**
```csharp
public class Asset : TenantEntity
{
    public string AssetCode { get; set; } // "LT-001"
    public string AssetType { get; set; } // "Laptop", "Mobile", "Chair"
    public string Make { get; set; }
    public string Model { get; set; }
    public string SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public AssetStatus Status { get; set; } // Available, Assigned, UnderMaintenance, Disposed
}

public class AssetAssignment : TenantEntity
{
    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public string AssignedCondition { get; set; } // "New", "Good", "Fair"
    public string ReturnedCondition { get; set; }
    public string ReturnNotes { get; set; }
}
```

**API Endpoints:**
```
POST   /assets                        [TA]  Add new asset
GET    /assets                        [TA]  List all assets
POST   /assets/{id}/assign            [TA]  Issue asset to employee
POST   /assets/{id}/return            [TA]  Mark asset returned
GET    /employees/{id}/assets         [EMP] View own assigned assets
```

---

## 5. Cross-Cutting Concerns

### 5.1 Authentication & Authorization

**JWT Token Structure:**
```json
{
  "sub": "user-guid",
  "email": "john@acme.com",
  "tenantId": "tenant-guid",
  "role": "TA",
  "permissions": ["employees.create", "employees.read", "payroll.read"],
  "exp": 1234567890
}
```

**Permission Matrix:**

| Resource | Action | SA | TA | MGR | PA | EMP |
|----------|--------|----|----|-----|----|----|
| Tenants | Create | ✓ | ✗ | ✗ | ✗ | ✗ |
| Employees | Create | ✗ | ✓ | ✗ | ✗ | ✗ |
| Employees | Read | ✗ | ✓ | ✓(Team) | ✗ | ✓(Self) |
| Leave Requests | Approve | ✗ | ✓ | ✓(Team) | ✗ | ✗ |
| Payroll | Run | ✗ | ✗ | ✗ | ✓ | ✗ |
| Payslips | Read | ✗ | ✗ | ✗ | ✗ | ✓(Self) |

**Authorization Middleware:**
```csharp
[Authorize]
[RequirePermission("employees.create")]
public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
{
    // Permission checked by attribute
    // Tenant context already set by TenantMiddleware
}

public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

public class PermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userPermissions = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "permissions")?.Value
            .Split(',');

        if (!userPermissions.Contains(_permission))
        {
            context.Result = new ForbidResult();
        }
    }
}
```

---

### 5.2 Caching Strategy

**Redis Cache Layers:**

1. **Permission Cache** (TTL: 1 hour)
   - Key: `permissions:{userId}`
   - Value: List of permission strings

2. **Schema Cache** (TTL: 24 hours)
   - Key: `schema:{module}:{regionCode}`
   - Value: JSON schema definition

3. **Tenant Config Cache** (TTL: 1 hour)
   - Key: `tenant:{tenantId}:config`
   - Value: Region, Currency, Timezone

**Implementation:**
```csharp
public class CachedSchemaService
{
    private readonly IDistributedCache _cache;
    private readonly SchemaRepository _repo;

    public async Task<SchemaDefinition> GetSchemaAsync(string module, string regionCode)
    {
        var cacheKey = $"schema:{module}:{regionCode}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<SchemaDefinition>(cached);

        var schema = await _repo.GetSchemaAsync(module, regionCode);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(schema),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });

        return schema;
    }
}
```

---

### 5.3 Audit Logging

**Every state-changing operation must be logged.**

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // Null for platform-level actions
    public Guid UserId { get; set; }
    public string Action { get; set; } // "Employee.Created", "Leave.Approved"
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string OldValue { get; set; } // JSON snapshot
    public string NewValue { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}
```

**Automatic Capture via EF Core Interceptor:**
```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                Action = $"{entry.Entity.GetType().Name}.{entry.State}",
                EntityType = entry.Entity.GetType().Name,
                OldValue = JsonSerializer.Serialize(entry.OriginalValues.ToObject()),
                NewValue = JsonSerializer.Serialize(entry.CurrentValues.ToObject())
            };
            eventData.Context.Set<AuditLog>().Add(auditLog);
        }

        return base.SavingChanges(eventData, result);
    }
}
```

---

### 5.4 Error Handling

**Global Exception Handler (Middleware):**
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, 404, ex.Message);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, 400, ex.Message, ex.Errors);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, 401, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, 500, "An error occurred");
        }
    }

    private async Task WriteErrorAsync(HttpContext context, int statusCode, string message, object errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

---

## 6. Database Design

### 6.1 Schema Overview

**Platform Schema (Global):**
- tenants
- regions
- currencies
- timezones
- audit_logs (platform-level)

**Tenant Schema (Row-Level Isolation):**
- employees
- departments
- designations
- leave_types, leave_policies, leave_balances, leave_requests
- attendance_records, attendance_regularizations, office_locations
- salary_structures, salary_components, employee_salaries, payroll_cycles, payslips
- assets, asset_assignments
- schema_definitions, schema_fields
- employee_profile_data

### 6.2 Indexes Strategy

**Critical Indexes:**
```sql
-- Tenant isolation (CRITICAL!)
CREATE INDEX idx_employees_tenant_id ON employees(tenant_id);
CREATE INDEX idx_leave_requests_tenant_id ON leave_requests(tenant_id);

-- Foreign keys
CREATE INDEX idx_employees_department_id ON employees(department_id);
CREATE INDEX idx_employees_manager_id ON employees(manager_id);

-- Query optimization
CREATE INDEX idx_attendance_employee_date ON attendance_records(employee_id, date);
CREATE INDEX idx_leave_requests_status ON leave_requests(status, tenant_id);
CREATE INDEX idx_payslips_cycle_employee ON payslips(payroll_cycle_id, employee_id);

-- Full-text search
CREATE INDEX idx_employees_search ON employees USING gin(to_tsvector('english', first_name || ' ' || last_name || ' ' || email));
```

---

## 7. Frontend Architecture

### 7.1 Project Structure

```
/src
  /app                          # Next.js App Router
    /[tenantSlug]               # Dynamic tenant routing
      /dashboard
      /employees
        /page.tsx               # List view
        /[id]/page.tsx          # Detail view
        /new/page.tsx           # Create form
      /leave
      /attendance
      /payroll
      /assets
    /platform                   # Platform admin area
      /tenants
  /components
    /common                     # Reusable UI components
      /DataTable
      /DynamicForm
      /PermissionGate
    /modules
      /employees
        /EmployeeCard.tsx
        /OrgChart.tsx
  /lib
    /api                        # API client
      /axios-instance.ts
      /endpoints/
        /employees.ts
        /leave.ts
    /hooks                      # React hooks
      /usePermissions.ts
      /useTenant.ts
    /utils
      /date-formatter.ts
      /currency-formatter.ts
  /types                        # TypeScript definitions
    /employee.ts
    /leave.ts
```

### 7.2 Dynamic Form Rendering

```typescript
// components/common/DynamicForm.tsx
import { useQuery } from '@tanstack/react-query';
import { Form, Input, Select, DatePicker } from 'antd';

interface DynamicFormProps {
  module: string;
  onSubmit: (values: any) => void;
}

export function DynamicForm({ module, onSubmit }: DynamicFormProps) {
  const { data: schema } = useQuery({
    queryKey: ['schema', module],
    queryFn: () => api.getSchema(module),
  });

  if (!schema) return <Spin />;

  return (
    <Form onFinish={onSubmit}>
      {schema.fields.map((field) => (
        <Form.Item
          key={field.fieldKey}
          name={field.fieldKey}
          label={field.label}
          rules={[
            { required: field.isRequired, message: `${field.label} is required` },
            ...(field.validationRules ? JSON.parse(field.validationRules) : []),
          ]}
        >
          {renderField(field)}
        </Form.Item>
      ))}
      <Button htmlType="submit">Submit</Button>
    </Form>
  );
}

function renderField(field: SchemaField) {
  switch (field.fieldType) {
    case 'text':
      return <Input />;
    case 'number':
      return <InputNumber />;
    case 'date':
      return <DatePicker />;
    case 'dropdown':
      const options = JSON.parse(field.optionsSource);
      return <Select options={options} />;
    default:
      return <Input />;
  }
}
```

### 7.3 Permission-Based UI

```typescript
// components/common/PermissionGate.tsx
import { usePermissions } from '@/lib/hooks/usePermissions';

interface PermissionGateProps {
  permission: string;
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  const { hasPermission } = usePermissions();

  if (!hasPermission(permission)) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

// Usage
<PermissionGate permission="employees.create">
  <Button onClick={openCreateModal}>Add Employee</Button>
</PermissionGate>
```

---

## 8. Deployment Architecture

### 8.1 Infrastructure (Azure-based)

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Load Balancer                       │
└───────────────────────┬─────────────────────────────────────┘
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼────────┐              ┌───────▼────────┐
│   App Service  │              │   App Service  │
│   (Frontend)   │              │   (Backend)    │
│   Next.js      │              │   .NET Core    │
└───────┬────────┘              └───────┬────────┘
        │                               │
        └───────────────┬───────────────┘
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼────────┐              ┌───────▼────────┐
│  PostgreSQL    │              │  Redis Cache   │
│  Flexible Srv  │              │                │
└────────────────┘              └────────────────┘
        │
┌───────▼────────┐
│  Blob Storage  │
│  (PDF, Files)  │
└────────────────┘
```

### 8.2 Environment Configuration

**Development:**
- Frontend: `localhost:3000`
- Backend: `localhost:5001`
- Database: Local PostgreSQL

**Staging:**
- Frontend: `https://staging-app.hrms.com`
- Backend: `https://staging-api.hrms.com`
- Database: Azure PostgreSQL (Staging)

**Production:**
- Frontend: `https://app.hrms.com`
- Backend: `https://api.hrms.com`
- Database: Azure PostgreSQL (Production with Read Replicas)

### 8.3 CI/CD Pipeline

```yaml
# .github/workflows/backend.yml
name: Backend CI/CD

on:
  push:
    branches: [main, staging]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
      - name: Publish
        run: dotnet publish -c Release -o ./publish
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'hrms-backend-api'
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 9. Security Considerations

### 9.1 Data Protection

1. **Encryption at Rest**: Azure Blob Storage encryption, PostgreSQL Transparent Data Encryption
2. **Encryption in Transit**: HTTPS/TLS 1.3 enforced
3. **Sensitive Data Hashing**: Password hashing with bcrypt (salt rounds: 12)
4. **PII Protection**: Payroll data encrypted with tenant-specific keys

### 9.2 OWASP Top 10 Mitigations

| Vulnerability | Mitigation |
|--------------|------------|
| SQL Injection | EF Core parameterized queries, no raw SQL |
| XSS | Ant Design auto-escapes output, CSP headers |
| CSRF | SameSite cookies, CSRF tokens on forms |
| Broken Access Control | RBAC middleware, tenant context validation |
| Security Misconfiguration | Hardened headers, no default credentials |
| Sensitive Data Exposure | HTTPS only, encrypted storage |

### 9.3 Rate Limiting

```csharp
// Startup.cs
services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## 10. Monitoring & Observability

### 10.1 Logging Stack

- **Application Logs**: Serilog -> Seq (structured logging)
- **Performance Monitoring**: Application Insights
- **Error Tracking**: Sentry (Frontend & Backend)

### 10.2 Key Metrics

- API response times (P95, P99)
- Database query performance
- Cache hit rates
- Failed login attempts
- Payroll processing duration

### 10.3 Alerting

- Critical: Database connection failures, Auth service down
- High: API error rate > 5%, Disk space < 10%
- Medium: Cache misses > 30%, Slow queries (> 2s)

---

## 11. Performance Optimization

### 11.1 Backend Optimizations

1. **Query Optimization**: Use `.Include()` for eager loading, avoid N+1 queries
2. **Pagination**: Default page size 20, max 100
3. **Async/Await**: All I/O operations use async
4. **Connection Pooling**: Max pool size 100

```csharp
// Good: Eager loading
var employees = await _context.Employees
    .Include(e => e.Department)
    .Include(e => e.Manager)
    .Where(e => e.TenantId == tenantId)
    .ToListAsync();

// Bad: N+1 query
var employees = await _context.Employees.ToListAsync();
foreach (var emp in employees)
{
    var dept = await _context.Departments.FindAsync(emp.DepartmentId); // N queries!
}
```

### 11.2 Frontend Optimizations

1. **Code Splitting**: Dynamic imports for heavy components
2. **Image Optimization**: Next.js `<Image>` with WebP
3. **Data Fetching**: React Server Components for initial data
4. **Caching**: TanStack Query with staleTime

```typescript
// app/employees/page.tsx (Server Component)
import { getEmployees } from '@/lib/api/employees';

export default async function EmployeesPage() {
  const employees = await getEmployees(); // Fetched on server

  return <EmployeeList initialData={employees} />;
}

// components/EmployeeList.tsx (Client Component)
'use client';
import { useQuery } from '@tanstack/react-query';

export function EmployeeList({ initialData }) {
  const { data } = useQuery({
    queryKey: ['employees'],
    queryFn: getEmployees,
    initialData,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return <Table dataSource={data} />;
}
```

---

## 12. Testing Strategy

### 12.1 Backend Testing

**Unit Tests** (xUnit + Moq):
```csharp
public class LeaveServiceTests
{
    [Fact]
    public async Task ApplyLeave_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var mockRepo = new Mock<ILeaveRepository>();
        mockRepo.Setup(r => r.GetBalanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(5);
        var service = new LeaveService(mockRepo.Object);

        // Act
        var result = await service.ApplyLeaveAsync(employeeId, new LeaveRequestDto { DaysCount = 10 });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Insufficient balance", result.ErrorMessage);
    }
}
```

**Integration Tests** (WebApplicationFactory):
```csharp
public class EmployeesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetEmployees_WithValidToken_ReturnsEmployees()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _validToken);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
        employees.Should().NotBeEmpty();
    }
}
```

### 12.2 Frontend Testing

**Component Tests** (Jest + React Testing Library):
```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { EmployeeForm } from './EmployeeForm';

test('submits form with valid data', async () => {
  const onSubmit = jest.fn();
  render(<EmployeeForm onSubmit={onSubmit} />);

  fireEvent.change(screen.getByLabelText('First Name'), { target: { value: 'John' } });
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'john@test.com' } });
  fireEvent.click(screen.getByText('Submit'));

  await waitFor(() => {
    expect(onSubmit).toHaveBeenCalledWith({ firstName: 'John', email: 'john@test.com' });
  });
});
```

**E2E Tests** (Playwright):
```typescript
import { test, expect } from '@playwright/test';

test('employee can apply for leave', async ({ page }) => {
  await page.goto('/login');
  await page.fill('[name="email"]', 'employee@test.com');
  await page.fill('[name="password"]', 'password');
  await page.click('button[type="submit"]');

  await page.goto('/leave/apply');
  await page.selectOption('[name="leaveType"]', 'Annual Leave');
  await page.fill('[name="startDate"]', '2024-03-01');
  await page.fill('[name="endDate"]', '2024-03-05');
  await page.click('button[type="submit"]');

  await expect(page.locator('text=Leave request submitted')).toBeVisible();
});
```

---

## 13. Migration from Monolith to Microservices (Future)

When the system scales, we can extract modules into microservices:

### 13.1 Service Boundaries

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Employee Svc   │────▶│   Leave Svc     │────▶│  Payroll Svc    │
│  (Core HR)      │     │  (Leave Mgmt)   │     │  (Payroll)      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
         │                       │                       │
         └───────────────────────┴───────────────────────┘
                                 │
                        ┌────────▼────────┐
                        │   Event Bus     │
                        │  (Azure Service │
                        │     Bus)        │
                        └─────────────────┘
```

### 13.2 Data Consistency

- **Event Sourcing**: Critical events stored in event log
- **Saga Pattern**: Distributed transactions (e.g., Payroll run triggers multiple services)
- **CQRS**: Separate read/write models for reporting

---

## 14. Cost Estimation (MVP - 6 Months)

| Resource | Tier | Monthly Cost (USD) |
|----------|------|-------------------|
| Azure App Service (Frontend) | Standard S1 | $75 |
| Azure App Service (Backend) | Standard S2 | $150 |
| PostgreSQL Flexible Server | Burstable B2s | $50 |
| Redis Cache | Basic C1 | $40 |
| Blob Storage | Hot Tier (100GB) | $20 |
| Application Insights | Pay-as-you-go | $30 |
| **Total** | | **$365/month** |

**Development Team (6 months):**
- 2 Full-stack Developers: $10,000/month × 2 × 6 = $120,000
- 1 UI/UX Designer: $8,000/month × 6 = $48,000
- 1 DevOps Engineer (Part-time): $5,000/month × 6 = $30,000

**Total MVP Cost: ~$200,000**

---

## 15. Success Metrics

### 15.1 Technical KPIs
- API response time < 200ms (P95)
- Database query time < 100ms (P95)
- Frontend page load < 2s
- 99.9% uptime (43 minutes downtime/month)

### 15.2 Business KPIs
- 10 paying tenants within 6 months
- Average 100 employees per tenant
- < 5% churn rate
- NPS > 40

---

## 16. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|------------|------------|
| Multi-tenancy data leak | Critical | Low | Automated tests for tenant isolation, code reviews |
| Complex payroll logic | High | Medium | Modular design, extensive unit tests, pilot with 1 tenant |
| Performance issues at scale | High | Medium | Caching layer, database indexing, load testing |
| Region-specific compliance | High | Medium | Schema engine flexibility, legal consultation per region |
| Scope creep | High | High | Strict MVP boundaries, change control process |

---

## 17. Next Steps

### Phase 1: Foundation (Weeks 1-4)
- [ ] Setup Next.js + .NET Core projects
- [ ] Database schema design
- [ ] Authentication & tenant middleware
- [ ] Platform module (Tenant CRUD)

### Phase 2: Core HR (Weeks 5-8)
- [ ] Employee management
- [ ] Organization structure
- [ ] Schema engine implementation
- [ ] Dynamic forms

### Phase 3: Leave & Attendance (Weeks 9-12)
- [ ] Leave policies & workflow
- [ ] Attendance tracking with geolocation
- [ ] Regularization workflow

### Phase 4: Payroll & Assets (Weeks 13-16)
- [ ] Salary structure definition
- [ ] Payroll calculation engine
- [ ] PDF payslip generation
- [ ] Asset tracking

### Phase 5: Testing & Launch (Weeks 17-20)
- [ ] Integration testing
- [ ] Security audit
- [ ] Performance optimization
- [ ] Pilot deployment with 2 tenants

---

## 18. Appendix

### A. Sample API Endpoints

Full API documentation: [Link to Swagger/OpenAPI spec]

### B. Database ERD

[Include Mermaid diagram or link to dbdiagram.io]

### C. UI Wireframes

[Link to Figma/Adobe XD]

### D. Glossary

- **Tenant**: A client company using the HRMS
- **Region**: Geographic location with specific regulations (UAE, India, US)
- **Accrual**: Automatic addition of leave balance
- **Regularization**: Process to correct attendance records

---

**Document Version**: 1.0
**Last Updated**: 2026-02-10
**Maintained By**: Development Team
**Review Cycle**: Quarterly
