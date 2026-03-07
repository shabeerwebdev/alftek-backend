using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Data.Seeding;

/// <summary>
/// Seeds initial data for the HRMS system
/// Includes regions and optionally demo data for development
/// </summary>
public class DataSeeder
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(HrmsDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Fixed UUIDs for demo seed data — must stay in sync with seed-demo-data.sql
    // -----------------------------------------------------------------------
    private static readonly Guid DemoTenantId   = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly Guid DemoSaUserId   = Guid.Parse("10000000-0000-4000-8000-000000000002");
    private static readonly Guid DemoAdminUserId = Guid.Parse("10000000-0000-4000-8000-000000000003");
    private static readonly Guid DemoMgrUserId  = Guid.Parse("10000000-0000-4000-8000-000000000004");
    private static readonly Guid DemoAdminEmpId = Guid.Parse("10000000-0000-4000-8000-000000000005");

    /// <summary>
    /// Seeds all initial data (regions, and optionally demo data)
    /// </summary>
    /// <param name="seedDemoData">Whether to seed demo data (tenants, users, etc.)</param>
    public async Task SeedAsync(bool seedDemoData = false)
    {
        _logger.LogInformation("Starting data seeding...");

        try
        {
            // Seed regions (CRITICAL - required for tenant onboarding)
            await SeedRegionsAsync();

            // Seed statutory contribution rules (depends on regions)
            await Seeders.StatutoryContributionSeeder.SeedAsync(_context);

            // Optionally seed demo data for development/testing
            if (seedDemoData)
            {
                await SeedSuperAdminAsync();
                await SeedDemoDataAsync();
            }

            _logger.LogInformation("✓ Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed data");
            throw;
        }
    }

    /// <summary>
    /// Seeds the platform-level Super Admin (TenantId = null).
    /// Idempotent — safe to run multiple times.
    /// Login: sa@myhrms.com / Admin@123
    /// </summary>
    private async Task SeedSuperAdminAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Id == DemoSaUserId))
        {
            _logger.LogInformation("Super Admin already exists. Skipping.");
            return;
        }

        var sa = new User
        {
            Id           = DemoSaUserId,
            TenantId     = null,
            Email        = "sa@myhrms.com",
            FirstName    = "Super",
            LastName     = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role         = UserRole.SA,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _context.Users.Add(sa);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Super Admin seeded: sa@myhrms.com / Admin@123");
    }

    /// <summary>
    /// Seeds the 3 supported regions: UAE, USA, India
    /// Idempotent - safe to run multiple times
    /// </summary>
    private async Task SeedRegionsAsync()
    {
        _logger.LogInformation("Seeding regions...");

        // Check if regions already exist
        if (await _context.Regions.AnyAsync())
        {
            _logger.LogInformation("Regions already exist. Skipping region seeding.");
            return;
        }

        var regions = new List<Region>
        {
            // United Arab Emirates (UAE)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "UAE",
                Name = "United Arab Emirates",
                CurrencyCode = "AED",
                DateFormat = "dd/MM/yyyy",
                Direction = "rtl", // Right-to-Left for Arabic
                LanguageCode = "ar",
                Timezone = "Asia/Dubai",
                CreatedAt = DateTime.UtcNow
            },

            // United States of America (USA)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "USA",
                Name = "United States",
                CurrencyCode = "USD",
                DateFormat = "MM/dd/yyyy",
                Direction = "ltr", // Left-to-Right
                LanguageCode = "en",
                Timezone = "America/New_York",
                CreatedAt = DateTime.UtcNow
            },

            // India (IND)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "IND",
                Name = "India",
                CurrencyCode = "INR",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "hi",
                Timezone = "Asia/Kolkata",
                CreatedAt = DateTime.UtcNow
            },

            // Malaysia (MYS)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "MYS",
                Name = "Malaysia",
                CurrencyCode = "MYR",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "ms",
                Timezone = "Asia/Kuala_Lumpur",
                CreatedAt = DateTime.UtcNow
            },

            // Singapore (SGP)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "SGP",
                Name = "Singapore",
                CurrencyCode = "SGD",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "en",
                Timezone = "Asia/Singapore",
                CreatedAt = DateTime.UtcNow
            },

            // United Kingdom (GBR)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "GBR",
                Name = "United Kingdom",
                CurrencyCode = "GBP",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "en",
                Timezone = "Europe/London",
                CreatedAt = DateTime.UtcNow
            },

            // Australia (AUS)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "AUS",
                Name = "Australia",
                CurrencyCode = "AUD",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "en",
                Timezone = "Australia/Sydney",
                CreatedAt = DateTime.UtcNow
            },

            // Canada (CAN)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "CAN",
                Name = "Canada",
                CurrencyCode = "CAD",
                DateFormat = "dd/MM/yyyy",
                Direction = "ltr",
                LanguageCode = "en",
                Timezone = "America/Toronto",
                CreatedAt = DateTime.UtcNow
            },

            // Philippines (PHL)
            new Region
            {
                Id = Guid.NewGuid(),
                Code = "PHL",
                Name = "Philippines",
                CurrencyCode = "PHP",
                DateFormat = "MM/dd/yyyy",
                Direction = "ltr",
                LanguageCode = "en",
                Timezone = "Asia/Manila",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Regions.AddRangeAsync(regions);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Successfully seeded {Count} regions: {Regions}",
            regions.Count,
            string.Join(", ", regions.Select(r => r.Code)));
    }

    /// <summary>
    /// Seeds demo data for development/testing purposes.
    /// Creates a demo tenant with admin user, departments, designations, locations, leave types,
    /// and the admin employee record (EMP001). All IDs are fixed/deterministic so that
    /// seed-demo-data.sql (run post-startup) can build on top of this foundation.
    ///
    /// Demo credentials:
    ///   SA     : sa@myhrms.com        / Admin@123
    ///   Admin  : admin@demo.myhrms.com / Demo@123
    ///   Manager: manager@demo.myhrms.com / Demo@123
    /// </summary>
    private async Task SeedDemoDataAsync()
    {
        _logger.LogInformation("Seeding demo data...");

        // Check if demo tenant already exists
        if (await _context.Tenants.AnyAsync(t => t.Subdomain == "demo"))
        {
            _logger.LogInformation("Demo tenant already exists. Skipping demo data seeding.");
            return;
        }

        var uaeRegion = await _context.Regions.FirstOrDefaultAsync(r => r.Code == "UAE");
        if (uaeRegion == null)
        {
            _logger.LogWarning("UAE region not found. Skipping demo data seeding.");
            return;
        }

        // 1. Create demo tenant (fixed ID)
        var tenant = new Tenant
        {
            Id                = DemoTenantId,
            Name              = "Demo Company",
            Subdomain         = "demo",
            RegionId          = uaeRegion.Id,
            IsActive          = true,
            SubscriptionStart = DateTime.UtcNow,
            SubscriptionEnd   = DateTime.UtcNow.AddYears(5),
            CreatedAt         = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        // 2. Create admin user — fixed ID, password: Demo@123
        var adminUser = new User
        {
            Id           = DemoAdminUserId,
            TenantId     = DemoTenantId,
            Email        = "admin@demo.myhrms.com",
            FirstName    = "Demo",
            LastName     = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123"),
            Role         = UserRole.TA,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _context.Users.Add(adminUser);

        // 3. Create manager user — fixed ID, password: Demo@123
        var managerUser = new User
        {
            Id           = DemoMgrUserId,
            TenantId     = DemoTenantId,
            Email        = "manager@demo.myhrms.com",
            FirstName    = "Demo",
            LastName     = "Manager",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123"),
            Role         = UserRole.MGR,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _context.Users.Add(managerUser);

        await _context.SaveChangesAsync();

        // 4. Departments
        var engineering = new Department { Id = Guid.NewGuid(), TenantId = DemoTenantId, Name = "Engineering",     Code = "ENG"   };
        var hr          = new Department { Id = Guid.NewGuid(), TenantId = DemoTenantId, Name = "Human Resources", Code = "HR"    };
        var finance     = new Department { Id = Guid.NewGuid(), TenantId = DemoTenantId, Name = "Finance",         Code = "FIN"   };
        var sales       = new Department { Id = Guid.NewGuid(), TenantId = DemoTenantId, Name = "Sales",           Code = "SALES" };
        _context.Departments.AddRange(engineering, hr, finance, sales);

        // 5. Designations
        var swe     = new Designation { Id = Guid.NewGuid(), TenantId = DemoTenantId, Title = "Software Engineer",        Code = "SWE",    Level = 3 };
        var srSwe   = new Designation { Id = Guid.NewGuid(), TenantId = DemoTenantId, Title = "Senior Software Engineer", Code = "SR-SWE", Level = 4 };
        var manager = new Designation { Id = Guid.NewGuid(), TenantId = DemoTenantId, Title = "Manager",                 Code = "MGR",    Level = 5 };
        var analyst = new Designation { Id = Guid.NewGuid(), TenantId = DemoTenantId, Title = "Analyst",                 Code = "ANL",    Level = 3 };
        _context.Designations.AddRange(swe, srSwe, manager, analyst);

        // 6. Locations
        var dxbHq = new Location
        {
            Id = Guid.NewGuid(), TenantId = DemoTenantId,
            Name = "Dubai Head Office", Code = "DXB-HQ",
            Address = "Sheikh Zayed Road", City = "Dubai", Country = "UAE",
            Latitude = 25.2048m, Longitude = 55.2708m, RadiusMeters = 200,
            IsActive = true
        };
        var abuDhabi = new Location
        {
            Id = Guid.NewGuid(), TenantId = DemoTenantId,
            Name = "Abu Dhabi Branch", Code = "AUH",
            Address = "Corniche Road", City = "Abu Dhabi", Country = "UAE",
            Latitude = 24.4539m, Longitude = 54.3773m, RadiusMeters = 150,
            IsActive = true
        };
        _context.Locations.AddRange(dxbHq, abuDhabi);

        // 7. Leave types
        var annualLeave = new LeaveType
        {
            Id = Guid.NewGuid(), TenantId = DemoTenantId,
            Name = "Annual Leave", Code = "AL",
            MaxDaysPerYear = 30, IsCarryForward = true, RequiresApproval = true, IsActive = true
        };
        var sickLeave = new LeaveType
        {
            Id = Guid.NewGuid(), TenantId = DemoTenantId,
            Name = "Sick Leave", Code = "SL",
            MaxDaysPerYear = 15, IsCarryForward = false, RequiresApproval = true, IsActive = true
        };
        var casualLeave = new LeaveType
        {
            Id = Guid.NewGuid(), TenantId = DemoTenantId,
            Name = "Casual Leave", Code = "CL",
            MaxDaysPerYear = 10, IsCarryForward = false, RequiresApproval = false, IsActive = true
        };
        _context.LeaveTypes.AddRange(annualLeave, sickLeave, casualLeave);

        await _context.SaveChangesAsync();

        // 8. Admin Employee record (EMP001) — fixed ID so seed-demo-data.sql can UPDATE it
        //    Department/designation/location will be filled in by seed-demo-data.sql
        var adminEmployee = new Employee
        {
            Id           = DemoAdminEmpId,
            TenantId     = DemoTenantId,
            UserId       = DemoAdminUserId,
            EmployeeCode = "EMP001",
            FirstName    = "Demo",
            LastName     = "Admin",
            Email        = "admin@demo.myhrms.com",
            JoiningDate  = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Status       = EmployeeStatus.Active,
            CreatedAt    = DateTime.UtcNow
        };
        _context.Employees.Add(adminEmployee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Demo data seeded successfully");
        _logger.LogInformation("  Demo Tenant : {TenantName} (ID: {Id})", tenant.Name, DemoTenantId);
        _logger.LogInformation("  SA Login    : sa@myhrms.com / Admin@123");
        _logger.LogInformation("  Admin Login : admin@demo.myhrms.com / Demo@123");
        _logger.LogInformation("  Mgr Login   : manager@demo.myhrms.com / Demo@123");
        _logger.LogInformation("  Departments : Engineering, HR, Finance, Sales");
        _logger.LogInformation("  Locations   : Dubai HQ, Abu Dhabi Branch");
        _logger.LogInformation("  Leave Types : Annual (30d), Sick (15d), Casual (10d)");
        _logger.LogInformation("  (seed-demo-data.sql will add employees, payroll, assets, etc.)");
    }
}

/// <summary>
/// Extension methods for data seeding
/// </summary>
public static class DataSeederExtensions
{
    /// <summary>
    /// Seeds initial data for the application
    /// Call this from Program.cs or a separate seeding command
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="seedDemoData">Whether to seed demo data for development</param>
    public static async Task SeedDataAsync(this IApplicationBuilder app, bool seedDemoData = false)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var logger = services.GetRequiredService<ILogger<DataSeeder>>();

            // Ensure database is created and migrations are applied
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException)
            {
                // InMemory provider doesn't support migrations — fall back to EnsureCreated
                await context.Database.EnsureCreatedAsync();
            }

            var seeder = new DataSeeder(context, logger);
            await seeder.SeedAsync(seedDemoData);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<DataSeeder>>();
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
