using System.Reflection;
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

    public HrmsDbContext(
        DbContextOptions<HrmsDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
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

    #region SaveChanges Override - Auto-inject TenantId

    /// <summary>
    /// Overrides SaveChanges to automatically inject TenantId for new tenant-scoped entities
    /// This ensures all tenant data is properly isolated
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronous SaveChanges override
    /// </summary>
    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    #endregion
}
