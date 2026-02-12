# 🔄 Project Handover Document
## AlfTekPro Multi-Tenant HRMS - Backend Implementation

**Date**: 2026-02-10
**Context Limit Reached**: Session ending at 121K/200K tokens
**Phase**: Backend Foundation (Week 1 of 15-week plan)

---

## 📋 Project Overview

### Mission
Build a **production-ready Multi-Tenant HRMS** with row-level isolation supporting **UAE (RTL/Arabic)**, **USA (LTR/English)**, and **India (LTR/Hindi)** regions.

### Core Principles (CRITICAL - DO NOT DEVIATE)
1. **Backend First** - Complete backend foundation before any UI work (15 weeks)
2. **Database as Source of Truth** - Schema drives everything
3. **Simplicity Over Complexity** - No over-engineering
4. **JSONB for Flexibility** - Dynamic data stored in PostgreSQL JSONB (NOT complex normalized tables)
5. **Hardcoded Strategies** - Regional payroll logic in C# Strategy pattern (NOT formula parsers)
6. **Standard Monolith** - No microservices, no service bus, no read replicas

---

## 🏗️ Architecture (LOCKED IN)

### Pattern
**Modular Monolith with Clean Architecture**
```
API/Worker (Presentation)
    ↓
Infrastructure (Data Access, External Services)
    ↓
Application (Business Logic)
    ↓
Domain (Core - No Dependencies)
```

### Multi-Tenancy Strategy
**Row-Level Isolation** (Shared Database, Shared Schema)
- Every tenant-scoped table has `tenant_id` column
- EF Core Global Query Filters automatically inject `WHERE tenant_id = ?`
- TenantMiddleware extracts `tenant_id` from JWT claims
- SaveChanges Interceptor auto-populates `tenant_id` on insert
- **NEVER write manual WHERE tenant_id clauses** - rely on filters

### Key Design Decisions

#### 1. JSONB for Dynamic Data (NOT EAV Tables)
```sql
-- employees.dynamic_data (JSONB)
{
  "emirates_id": "784-1234-1234567-1",  -- UAE specific
  "pan_card": "ABCDE1234F"              -- India specific
}

-- form_templates.schema_json (JSONB)
{
  "fields": [
    {"key": "emirates_id", "type": "text", "required": true, "regex": "..."}
  ]
}

-- salary_structures.components_json (JSONB)
[
  {"component_id": "uuid", "calculation": "FIXED", "value": 5000},
  {"component_id": "uuid", "calculation": "PERCENT", "value": 0.4, "base": "BASIC"}
]
```

#### 2. Temporal Job History (SCD Type 2)
```sql
-- employee_job_history table
employee_id | department_id | valid_from | valid_to | change_type
uuid        | uuid          | 2024-01-01 | NULL     | PROMOTION
```
- `valid_to = NULL` indicates current record
- Tracks promotions, transfers, salary changes
- Separate from static Employee table for performance

#### 3. Unified Action Center
```sql
-- user_tasks table (decouples UI from modules)
owner_user_id | entity_type | entity_id | status | action_url
uuid          | "LEAVE"     | uuid      | Pending| "/leaves/view/123"
```
- Manager gets task when employee applies for leave
- Domain events trigger task creation
- Generic task entity used across all modules

#### 4. Shift-Based Attendance
```sql
-- shift_masters + employee_roster tables
-- Late calculation: clock_in > (shift.start_time + shift.grace_period_mins)
```

---

## 🛠️ Technology Stack (LOCKED - DO NOT CHANGE)

### Backend
- **.NET 8 Web API** (RESTful)
- **EF Core 8** with PostgreSQL provider
- **PostgreSQL 15** (JSONB, UUID, full-text search)
- **Redis 7** (Caching)
- **Hangfire** (Background jobs - payroll, bulk imports)
- **Azure Blob Storage** (Documents, payslips)
- **JWT Authentication** (tenant_id in claims)
- **BCrypt** (Password hashing)
- **Serilog** (Logging)
- **FluentValidation** (Input validation)
- **AutoMapper** (DTO mapping)
- **QuestPDF** (Payslip generation)
- **CsvHelper** (Bulk imports)

### Infrastructure
- **Docker Compose** (Local development)
- **Azure App Service** (Production deployment)
- **Azure Database for PostgreSQL** (Production DB)
- **Azure Redis Cache** (Production cache)
- **GitHub Actions** (CI/CD)

### Testing
- **xUnit** (Test framework)
- **FluentAssertions** (Assertions)
- **Moq + NSubstitute** (Mocking)
- **Testcontainers** (Integration tests with Docker)
- **Respawn** (Database cleanup between tests)

---

## ✅ What Has Been Completed

### Phase 1: Week 1 Progress (85% Complete)

#### 1. ✅ Project Infrastructure
- [x] Docker Compose configuration (`docker-compose.yml`)
  - PostgreSQL 15 with init script
  - Redis 7 with password
  - API service with hot reload
  - Worker service (Hangfire)
  - pgAdmin (optional)
  - Azurite (local Azure Storage)
- [x] Dockerfiles (multi-stage: development + production)
  - `backend/Dockerfile` (API)
  - `backend/Dockerfile.Worker` (Worker)
- [x] Environment configuration (`.env.example`)
- [x] Makefile (30+ convenient commands)
- [x] `.gitignore` and `.dockerignore`
- [x] README.md (comprehensive documentation)
- [x] QUICKSTART.md (5-minute setup guide)
- [x] Database initialization script (`scripts/init-db.sql`)

#### 2. ✅ .NET 8 Solution Structure
- [x] Solution file: `backend/AlfTekPro.HRMS.sln`
- [x] **5 Projects Created**:
  - `AlfTekPro.Domain` (Core - no dependencies)
  - `AlfTekPro.Application` (Business logic)
  - `AlfTekPro.Infrastructure` (Data access)
  - `AlfTekPro.API` (Web API)
  - `AlfTekPro.Worker` (Background jobs)
- [x] **2 Test Projects**:
  - `AlfTekPro.UnitTests`
  - `AlfTekPro.IntegrationTests`
- [x] All NuGet packages configured
- [x] Project references set up correctly

#### 3. ✅ Domain Layer (100% Complete)
- [x] Base classes:
  - `BaseEntity` (Id, CreatedAt, UpdatedAt)
  - `ITenantEntity` (marker interface)
  - `BaseTenantEntity` (combines both)
- [x] Enums:
  - `UserRole` (SA, TA, MGR, PA, EMP)
  - `EmployeeStatus` (Active, Notice, Exited)
  - `AttendanceStatus` (Present, Absent, HalfDay, OnLeave)
  - `LeaveRequestStatus` (Pending, Approved, Rejected)
- [x] **ALL 24 Entity Models Created**:

**Platform Module (4 entities)**:
- `Region` - Localization settings (Code, Name, CurrencyCode, DateFormat, Direction, LanguageCode, Timezone)
- `Tenant` - Multi-tenant orgs (Name, Subdomain, RegionId, IsActive, SubscriptionStart/End)
- `User` - Authentication (TenantId?, Email, PasswordHash, Role, IsActive, LastLogin)
- `FormTemplate` - Dynamic forms (RegionId, Module, SchemaJson as JSONB, IsActive)

**Core HR Module (5 entities)**:
- `Department` - Hierarchical structure (Name, ParentDepartmentId?)
- `Designation` - Job titles (Title, Level)
- `Location` - Offices with geofence (Name, Address, Latitude, Longitude, RadiusMeters)
- `Employee` - Profiles (UserId?, EmployeeCode, FirstName, LastName, Email, Phone, DateOfBirth, JoiningDate, Status, **DynamicData as JSONB**)
- `EmployeeJobHistory` - Temporal tracking (EmployeeId, DepartmentId?, DesignationId?, ReportingManagerId?, LocationId?, SalaryTierId?, ValidFrom, ValidTo?, ChangeType, ChangeReason, CreatedBy)

**Workforce Module (3 entities)**:
- `ShiftMaster` - Shift templates (Name, Code, StartTime, EndTime, GracePeriodMins, TotalHours, IsActive)
- `EmployeeRoster` - Shift assignments (EmployeeId, ShiftId, EffectiveDate)
- `AttendanceLog` - Clock-in/out (EmployeeId, Date, ClockIn?, ClockInIp, ClockInLatitude, ClockInLongitude, ClockOut?, Status, IsLate, LateByMinutes, IsRegularized, RegularizationReason)

**Leave Module (3 entities)**:
- `LeaveType` - Config (Name, Code, MaxDaysPerYear, IsCarryForward, RequiresApproval)
- `LeaveBalance` - Employee balances (EmployeeId, LeaveTypeId, Year, Accrued, Used)
- `LeaveRequest` - Applications (EmployeeId, LeaveTypeId, StartDate, EndDate, DaysCount, Reason, Status, ApprovedBy?, ApprovedAt?, ApproverComments)

**Workflow Module (1 entity)**:
- `UserTask` - Action center (OwnerUserId, Title, EntityType, EntityId, Status, ActionUrl, Priority, DueDate?, ActionedAt?)

**Payroll Module (4 entities)**:
- `SalaryComponent` - Earnings/deductions (TenantId?, Name, Code, Type, IsTaxable)
- `SalaryStructure` - Formulas (Name, **ComponentsJson as JSONB**)
- `PayrollRun` - Monthly runs (Month, Year, Status, S3PathPdfBundle?, ProcessedAt?)
- `Payslip` - Individual slips (PayrollRunId, EmployeeId, WorkingDays, PresentDays, GrossEarnings, TotalDeductions, NetPay, **BreakdownJson as JSONB**, PdfPath?)

**Assets Module (2 entities)**:
- `Asset` - Inventory (AssetCode, AssetType, Make, Model, SerialNumber, PurchaseDate, PurchasePrice, Status)
- `AssetAssignment` - Tracking (AssetId, EmployeeId, AssignedDate, ReturnedDate?, AssignedCondition, ReturnedCondition, ReturnNotes)

#### 4. ✅ Application Layer (Partial)
- [x] `ITenantContext` interface
- [x] `ApiResponse<T>` wrapper class
- [x] Folder structure created (Services, DTOs, Validators, Mappings)

#### 5. ✅ Infrastructure Layer (Partial)
- [x] `TenantContext` implementation
- [x] Folder structure created (Data/Contexts, Data/Configurations, Data/Interceptors, Services)

#### 6. ✅ API Layer (Basic Setup)
- [x] `Program.cs` with Swagger configuration
- [x] `appsettings.json` with connection strings
- [x] `appsettings.Development.json`
- [x] Health check endpoint configured
- [x] Folder structure (Controllers, Middleware, Extensions, Filters)

#### 7. ✅ Worker Layer (Basic Setup)
- [x] `Program.cs` with Hangfire placeholders
- [x] `Worker.cs` background service
- [x] `appsettings.json`

#### 8. ✅ Documentation
- [x] OpenAPI 3.0 specification (`openapi.yaml`) - 62 endpoints across 13 modules
- [x] System design document (`SYSTEM_DESIGN.md`)
- [x] Implementation plan (`.claude/plans/stateful-growing-tome.md`)
- [x] README.md with setup instructions
- [x] QUICKSTART.md

---

## 🎯 IMMEDIATE Next Steps (Priority Order)

### 1. ✅ CRITICAL: Create DbContext with Tenant Isolation
**File**: `backend/src/AlfTekPro.Infrastructure/Data/Contexts/HrmsDbContext.cs`

```csharp
public class HrmsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public HrmsDbContext(DbContextOptions<HrmsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // DbSets for all 24 entities
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    // ... all entities

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CRITICAL: Apply Global Query Filters for Multi-Tenancy
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private static readonly MethodInfo SetGlobalQueryMethod = typeof(HrmsDbContext)
        .GetMethod(nameof(SetGlobalQuery), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void SetGlobalQuery<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    // CRITICAL: SaveChanges Interceptor for Auto-Injecting TenantId
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _tenantContext.TenantId
                    ?? throw new InvalidOperationException("Tenant ID is required");
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### 2. ✅ Create Entity Configurations (Fluent API)
**Location**: `backend/src/AlfTekPro.Infrastructure/Data/Configurations/`

Create one configuration file per entity:
- `RegionConfiguration.cs`
- `TenantConfiguration.cs`
- `UserConfiguration.cs`
- etc. (24 total)

**Example** (`RegionConfiguration.cs`):
```csharp
public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("regions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        // Relationships
        builder.HasMany(r => r.Tenants)
            .WithOne(t => t.Region)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**JSONB Configuration Example** (`EmployeeConfiguration.cs`):
```csharp
builder.Property(e => e.DynamicData)
    .HasColumnType("jsonb")
    .HasColumnName("dynamic_data");
```

### 3. ✅ Create TenantMiddleware
**File**: `backend/src/AlfTekPro.API/Middleware/TenantMiddleware.cs`

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Extract tenant_id from JWT claims
        var tenantIdClaim = context.User.Claims
            .FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            tenantContext.SetTenantId(tenantId);
        }

        await _next(context);
    }
}
```

Register in `Program.cs`:
```csharp
// After app.UseAuthentication()
app.UseMiddleware<TenantMiddleware>();
```

### 4. ✅ Create Database Migration
```bash
# From backend/ directory
dotnet ef migrations add InitialCreate --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API

# Apply migration
dotnet ef database update --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API
```

### 5. ✅ Seed Initial Data
Create `DataSeeder.cs` in Infrastructure:
```csharp
public static class DataSeeder
{
    public static async Task SeedAsync(HrmsDbContext context)
    {
        if (!await context.Regions.AnyAsync())
        {
            var regions = new List<Region>
            {
                new() { Code = "UAE", Name = "United Arab Emirates", CurrencyCode = "AED",
                        DateFormat = "dd/MM/yyyy", Direction = "rtl", LanguageCode = "ar",
                        Timezone = "Asia/Dubai" },
                new() { Code = "USA", Name = "United States", CurrencyCode = "USD",
                        DateFormat = "MM/dd/yyyy", Direction = "ltr", LanguageCode = "en",
                        Timezone = "America/New_York" },
                new() { Code = "IND", Name = "India", CurrencyCode = "INR",
                        DateFormat = "dd/MM/yyyy", Direction = "ltr", LanguageCode = "hi",
                        Timezone = "Asia/Kolkata" }
            };
            await context.Regions.AddRangeAsync(regions);
            await context.SaveChangesAsync();
        }
    }
}
```

### 6. ✅ Implement First Controller (Auth Module)
**File**: `backend/src/AlfTekPro.API/Controllers/AuthController.cs`

Focus on:
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

### 7. ✅ Implement Tenant Onboarding
**File**: `backend/src/AlfTekPro.API/Controllers/TenantsController.cs`

Focus on:
- `POST /api/tenants/onboard` (public endpoint)
- `GET /api/tenants/check-domain` (public endpoint)

---

## ⚠️ CRITICAL Rules (DO NOT VIOLATE)

### Multi-Tenancy Rules
1. **NEVER write manual `WHERE tenant_id = ?` clauses** - Use EF Core Query Filters
2. **NEVER skip TenantMiddleware** - It must run after authentication
3. **ALWAYS inherit from `BaseTenantEntity`** for tenant-scoped entities
4. **Test tenant isolation** - Write unit tests to verify no cross-tenant data leakage

### Database Rules
1. **Use JSONB** for dynamic/flexible data (NOT separate tables)
2. **Use lowercase + snake_case** for table/column names
3. **Always use UUIDs** for primary keys (NOT auto-increment integers)
4. **Add indexes** on tenant_id for all tenant-scoped tables

### Code Quality Rules
1. **No over-engineering** - Keep it simple
2. **No premature abstractions** - Wait until you have 3+ use cases
3. **JSONB over normalization** - For region-specific or flexible data
4. **Hardcoded strategies** - For regional logic (NOT dynamic formula parsers)

### Git Commit Rules
1. **NEVER commit secrets** (.env file is in .gitignore)
2. **Follow commit conventions**: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`
3. **Include Claude co-author**: `Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>`

---

## 📂 Key File Locations

### Documentation
- Implementation plan: `.claude/plans/stateful-growing-tome.md`
- System design: `SYSTEM_DESIGN.md`
- API spec: `openapi.yaml`
- Setup guide: `README.md` and `QUICKSTART.md`
- **This handover**: `HANDOVER.md`

### Configuration
- Docker: `docker-compose.yml`, `backend/Dockerfile`, `backend/Dockerfile.Worker`
- Environment: `.env.example` (copy to `.env`)
- Database init: `scripts/init-db.sql`
- Makefile: `Makefile` (30+ commands)

### Code
- Solution: `backend/AlfTekPro.HRMS.sln`
- Domain entities: `backend/src/AlfTekPro.Domain/Entities/`
- Base classes: `backend/src/AlfTekPro.Domain/Common/`
- Application: `backend/src/AlfTekPro.Application/`
- Infrastructure: `backend/src/AlfTekPro.Infrastructure/`
- API: `backend/src/AlfTekPro.API/`
- Worker: `backend/src/AlfTekPro.Worker/`

---

## 🎯 15-Week Timeline (Reference)

**Current Phase**: Week 1 (85% complete)

- **Phase 1**: Weeks 1-3 - Foundation ← **WE ARE HERE**
- **Phase 2**: Weeks 4-6 - Workforce Management
- **Phase 3**: Weeks 7-8 - Workflow & Action Center
- **Phase 4**: Weeks 9-10 - Payroll
- **Phase 5**: Weeks 11-12 - Assets & Bulk Operations
- **Phase 6**: Weeks 13-15 - Testing & Deployment

---

## 🚨 What NOT to Do (Common Pitfalls)

1. ❌ **Don't create microservices** - We're building a monolith
2. ❌ **Don't add Azure Service Bus** - Not needed for MVP
3. ❌ **Don't create read replicas** - Premature optimization
4. ❌ **Don't build a formula parser** - Use Strategy pattern
5. ❌ **Don't create EAV tables** - Use JSONB
6. ❌ **Don't skip tenant isolation tests** - CRITICAL for security
7. ❌ **Don't work on frontend** - Backend first (15 weeks)
8. ❌ **Don't add features not in plan** - Stick to the 24 tables

---

## 💡 Quick Start Commands

```bash
# Start development environment
make up

# View logs
make logs-api

# Run migrations (when DbContext is ready)
make migrate

# Run tests (when tests are written)
make test

# Create backup
make backup

# Access database shell
make shell-postgres

# View all commands
make help
```

---

## 📞 Context for New Session

**What to say when you start**:
> "I'm continuing the AlfTekPro HRMS backend implementation. I've read the HANDOVER.md file. We're at 85% of Week 1 completion. The immediate next step is creating the DbContext with tenant isolation and EF Core configurations. Should I proceed with that?"

**Files to reference**:
1. `HANDOVER.md` (this file) - Overall context
2. `openapi.yaml` - API contracts
3. `.claude/plans/stateful-growing-tome.md` - Detailed plan
4. `SYSTEM_DESIGN.md` - Architecture details

---

## ✅ Session Handover Checklist

- [x] All 24 entity models created
- [x] Base classes and enums defined
- [x] Docker infrastructure ready
- [x] .NET solution structure complete
- [x] Project dependencies configured
- [x] NuGet packages installed
- [x] Folder structure established
- [ ] DbContext with Query Filters (NEXT)
- [ ] Entity Configurations (NEXT)
- [ ] TenantMiddleware (NEXT)
- [ ] Database migrations (NEXT)
- [ ] Data seeding (NEXT)

---

**End of Handover Document**

**Next Session Starts Here** →
Focus: `DbContext + Entity Configurations + TenantMiddleware + Migrations`
