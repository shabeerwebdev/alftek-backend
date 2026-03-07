using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Entities.Assets;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Entities.Workflow;
using AlfTekPro.Domain.Entities.Workforce;

namespace AlfTekPro.Infrastructure.Data.Contexts;

/// <summary>
/// Main database context for the HRMS application
/// Implements row-level multi-tenancy with automatic tenant isolation via EF Core Query Filters
/// </summary>
public class HrmsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService? _currentUserService;

    public HrmsDbContext(
        DbContextOptions<HrmsDbContext> options,
        ITenantContext tenantContext,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    #region Platform Module DbSets

    /// <summary>
    /// Regions configuration (Global - not tenant-scoped)
    /// </summary>
    public DbSet<Region> Regions => Set<Region>();

    /// <summary>
    /// Tenants (Organizations)
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Tenant company bank accounts (for payroll disbursement).</summary>
    public DbSet<TenantBankAccount> TenantBankAccounts => Set<TenantBankAccount>();

    /// <summary>
    /// Users with authentication details
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Refresh tokens for JWT authentication
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Dynamic form templates (region-specific)
    /// </summary>
    public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();

    /// <summary>
    /// Immutable audit trail for all entity changes
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Region-level statutory contribution rules (EPF, PF, SOCSO, ESI, CPF, etc.)
    /// </summary>
    public DbSet<StatutoryContributionRule> StatutoryContributionRules
        => Set<StatutoryContributionRule>();

    #endregion

    #region Core HR Module DbSets

    /// <summary>
    /// Departments (hierarchical structure)
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Job designations (titles)
    /// </summary>
    public DbSet<Designation> Designations => Set<Designation>();

    /// <summary>
    /// Office locations with geofence coordinates
    /// </summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>
    /// Employee profiles
    /// </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>
    /// Temporal job history (SCD Type 2) - tracks promotions, transfers, etc.
    /// </summary>
    public DbSet<EmployeeJobHistory> EmployeeJobHistories => Set<EmployeeJobHistory>();

    /// <summary>
    /// Employee salary payment bank accounts
    /// </summary>
    public DbSet<EmployeeBankAccount> EmployeeBankAccounts => Set<EmployeeBankAccount>();

    /// <summary>
    /// Employee emergency contacts
    /// </summary>
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    /// <summary>
    /// Tenant public holidays (excluded from working-day calculations)
    /// </summary>
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    /// <summary>
    /// HR documents attached to employees (metadata; files are in object store).
    /// </summary>
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    public DbSet<EmployeeQualification> EmployeeQualifications => Set<EmployeeQualification>();
    public DbSet<EmployeeWorkExperience> EmployeeWorkExperiences => Set<EmployeeWorkExperience>();
    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();

    #endregion

    #region Workforce Module DbSets

    /// <summary>
    /// Shift master data (templates)
    /// </summary>
    public DbSet<ShiftMaster> ShiftMasters => Set<ShiftMaster>();

    /// <summary>
    /// Employee shift roster (assignments)
    /// </summary>
    public DbSet<EmployeeRoster> EmployeeRosters => Set<EmployeeRoster>();

    /// <summary>
    /// Attendance logs (clock-in/out records)
    /// </summary>
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();

    /// <summary>
    /// Attendance regularization requests (employee-submitted corrections)
    /// </summary>
    public DbSet<AttendanceRegularizationRequest> AttendanceRegularizationRequests
        => Set<AttendanceRegularizationRequest>();

    #endregion

    #region Leave Module DbSets

    /// <summary>
    /// Leave types configuration
    /// </summary>
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    /// <summary>
    /// Employee leave balances
    /// </summary>
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    /// <summary>
    /// Leave applications and approvals
    /// </summary>
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    #endregion

    #region Workflow Module DbSets

    /// <summary>
    /// Action center tasks (unified approval workflow)
    /// </summary>
    public DbSet<UserTask> UserTasks => Set<UserTask>();

    #endregion

    #region Payroll Module DbSets

    /// <summary>
    /// Salary components (earnings and deductions)
    /// </summary>
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();

    /// <summary>
    /// Salary structures with component formulas (JSONB)
    /// </summary>
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();

    /// <summary>
    /// Monthly payroll runs
    /// </summary>
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();

    /// <summary>
    /// Individual employee payslips
    /// </summary>
    public DbSet<Payslip> Payslips => Set<Payslip>();

    /// <summary>Full and Final Settlements for exiting employees</summary>
    public DbSet<FnFSettlement> FnFSettlements => Set<FnFSettlement>();

    #endregion

    #region Assets Module DbSets

    /// <summary>
    /// Company assets inventory
    /// </summary>
    public DbSet<Asset> Assets => Set<Asset>();

    /// <summary>
    /// Asset assignments to employees
    /// </summary>
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();

    #endregion

    #region Model Configuration

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // CRITICAL: Apply Global Query Filters for Multi-Tenancy
        // This automatically adds "WHERE tenant_id = ?" to all queries for tenant-scoped entities
        ApplyGlobalQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters to all entities implementing ITenantEntity
    /// This ensures automatic tenant isolation - NEVER write manual WHERE tenant_id clauses
    /// </summary>
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private static readonly MethodInfo SetGlobalQueryMethod = typeof(HrmsDbContext)
        .GetMethod(nameof(SetGlobalQuery), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private void SetGlobalQuery<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        // Apply global query filter that references the current tenant context
        // EF Core will evaluate _tenantContext.TenantId at query execution time
        builder.Entity<T>().HasQueryFilter(e =>
            _tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId.Value);
    }

    #endregion

    #region SaveChanges Override - Auto-inject TenantId + Audit

    /// <summary>
    /// Overrides SaveChanges to automatically inject TenantId, timestamps, and write audit logs.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture audit entries BEFORE save so we can read OriginalValues for Modified/Deleted
        var auditEntries = CaptureAuditEntries();

        // Auto-populate TenantId for new tenant-scoped entities
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.TenantId == Guid.Empty)
            {
                if (_tenantContext.TenantId == null)
                {
                    throw new InvalidOperationException(
                        "Cannot create tenant-scoped entity without a tenant context. " +
                        "Ensure the user is authenticated and TenantMiddleware has set the tenant ID.");
                }

                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        // Auto-populate timestamps
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Persist audit entries via a direct base call to avoid recursive auditing
        if (auditEntries.Count > 0)
        {
            Set<AuditLog>().AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Synchronous SaveChanges override
    /// </summary>
    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Snapshots current ChangeTracker entries into AuditLog records.
    /// Must be called before base.SaveChangesAsync so OriginalValues are still available.
    /// </summary>
    private List<AuditLog> CaptureAuditEntries()
    {
        var entries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                        && e.Entity is not AuditLog))
        {
            var action = entry.State switch
            {
                EntityState.Added    => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted  => "Deleted",
                _                    => "Unknown"
            };

            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "unknown";

            string? oldValues = null;
            string? newValues = null;

            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                oldValues = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                newValues = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }

            var tenantId = entry.Entity is ITenantEntity te ? te.TenantId : (Guid?)null;

            entries.Add(new AuditLog
            {
                TenantId   = tenantId,
                UserId     = _currentUserService?.UserId,
                UserEmail  = _currentUserService?.UserEmail,
                Action     = action,
                EntityName = entry.Entity.GetType().Name,
                EntityId   = entityId,
                OldValues  = oldValues,
                NewValues  = newValues,
                CreatedAt  = DateTime.UtcNow
            });
        }

        return entries;
    }

    #endregion
}
