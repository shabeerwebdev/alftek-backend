using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Writers;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Common;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.Infrastructure.Data.Seeding;
using AlfTekPro.API.Middleware;
using AlfTekPro.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Database Configuration
builder.Services.AddDbContext<HrmsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            npgsqlOptions.MigrationsAssembly("AlfTekPro.Infrastructure");
        });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// HTTP context accessor — needed by CurrentUserService
builder.Services.AddHttpContextAccessor();

// Tenant Context (Scoped - one instance per HTTP request)
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Current user identity (reads from JWT claims)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Application Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Tenants.Interfaces.ITenantService, TenantService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Departments.Interfaces.IDepartmentService, DepartmentService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Designations.Interfaces.IDesignationService, DesignationService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Locations.Interfaces.ILocationService, LocationService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Employees.Interfaces.IEmployeeService, EmployeeService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.ShiftMasters.Interfaces.IShiftMasterService, ShiftMasterService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.EmployeeRosters.Interfaces.IEmployeeRosterService, EmployeeRosterService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.AttendanceLogs.Interfaces.IAttendanceLogService, AttendanceLogService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.LeaveTypes.Interfaces.ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.LeaveBalances.Interfaces.ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.LeaveRequests.Interfaces.ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.SalaryComponents.Interfaces.ISalaryComponentService, SalaryComponentService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.SalaryStructures.Interfaces.ISalaryStructureService, SalaryStructureService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.PayrollRuns.Interfaces.IPayrollRunService, PayrollRunService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Payslips.Interfaces.IPayslipService, PayslipService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.UserTasks.Interfaces.IUserTaskService, UserTaskService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Assets.Interfaces.IAssetService, AssetService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.FormTemplates.Interfaces.IFormTemplateService, FormTemplateService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.AuditLogs.Interfaces.IAuditLogService, AuditLogService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.EmployeeBankAccounts.Interfaces.IEmployeeBankAccountService, EmployeeBankAccountService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.EmergencyContacts.Interfaces.IEmergencyContactService, EmergencyContactService>();
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IEmailService, EmailService>();
builder.Services.AddSingleton<AlfTekPro.Application.Common.Interfaces.IFileStorageService, CloudflareR2StorageService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.PublicHolidays.Interfaces.IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IWorkingDayCalculatorService, WorkingDayCalculatorService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.AttendanceRegularization.Interfaces.IAttendanceRegularizationService, AttendanceRegularizationService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Payslips.Interfaces.IPayslipPdfService, PayslipPdfService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.StatutoryDeductions.Interfaces.IStatutoryDeductionService, StatutoryDeductionService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.FnFSettlements.Interfaces.IFnFSettlementService, FnFSettlementService>();
builder.Services.AddScoped<AlfTekPro.Infrastructure.Services.TenantRegionOnboardingService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.SetupWizard.Interfaces.ISetupWizardService, SetupWizardService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Reports.Interfaces.IReportService, ReportService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.BankPaymentFiles.Interfaces.IBankPaymentFileService, BankPaymentFileService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.Overtime.Interfaces.IOvertimeService, OvertimeService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.EmployeeDocuments.Interfaces.IEmployeeDocumentService, EmployeeDocumentService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.EmployeeProfile.Interfaces.IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<AlfTekPro.Application.Features.TenantBankAccounts.Interfaces.ITenantBankAccountService, TenantBankAccountService>();
// DynamicData validator + plugins
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IDynamicDataValidator, DynamicDataValidator>();
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IDynamicFieldCustomValidator, AlfTekPro.Infrastructure.Services.DynamicFieldValidators.UniqueWithinTenantValidator>();
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IDynamicFieldCustomValidator, AlfTekPro.Infrastructure.Services.DynamicFieldValidators.DbReferenceCheckValidator>();
builder.Services.AddScoped<AlfTekPro.Application.Common.Interfaces.IDynamicFieldCustomValidator, AlfTekPro.Infrastructure.Services.DynamicFieldValidators.ConditionalRequiredValidator>();

// FluentValidation - auto-validate incoming requests
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AlfTekPro.Application.Features.Tenants.Validators.TenantOnboardingValidator>();

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret is not configured");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer is not configured");
var jwtAudience = builder.Configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // Prevent remapping "role" → ClaimTypes.Role

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero, // Remove default 5 minute tolerance
        RoleClaimType = "role" // Map our custom "role" claim for [Authorize(Roles=...)]
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AlfTekPro Multi-Tenant HRMS API",
        Version = "v1",
        Description = "Multi-tenant Human Resource Management System with row-level isolation",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AlfTekPro Support",
            Email = "support@alftekpro.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Proprietary"
        }
    });

    // JWT Bearer Authentication for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your JWT token in the text input below."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Deterministic operationIds: ControllerName_ActionName (minimal API endpoints lack these keys)
    options.CustomOperationIds(e =>
    {
        e.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller);
        e.ActionDescriptor.RouteValues.TryGetValue("action", out var action);
        return controller != null && action != null
            ? $"{controller}_{action}"
            : e.ActionDescriptor.DisplayName;
    });

    // Explicit tag assignment by controller name
    options.TagActionsBy(api =>
    {
        api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller);
        return new[] { controller ?? api.HttpMethod ?? "Other" };
    });

    // Clear JWT security from AllowAnonymous endpoints
    options.OperationFilter<AllowAnonymousOperationFilter>();

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Lowercase all route URLs for deterministic OpenAPI paths
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

// Rate Limiting (brute-force protection on auth endpoints)
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// Hangfire — registers IBackgroundJobClient/IRecurringJobManager for enqueuing jobs from the API
// The Worker service is the actual job processor; the API only enqueues
var isSwaggerGen = string.Equals(Environment.GetEnvironmentVariable("SWAGGER_GEN"), "true", StringComparison.OrdinalIgnoreCase);
if (!isSwaggerGen)
{
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
    });
}
else
{
    builder.Services.AddHangfire(config =>
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings()
              .UseInMemoryStorage());
}

// Redis — IConnectionMultiplexer (singleton) + IDistributedCache + ICacheService wrapper
var redisConnectionString = isSwaggerGen ? null : builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    var mux = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "alftekpro:";
    });
}
else
{
    // Fall back to in-memory cache when Redis is not configured (local dev without Redis)
    builder.Services.AddDistributedMemoryCache();
    // Register a no-op multiplexer sentinel so RedisCacheService can still be resolved
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false"));
}
builder.Services.AddScoped<ICacheService, AlfTekPro.Infrastructure.Common.RedisCacheService>();

// Health checks
builder.Services.AddHealthChecks();

// CORS — restrict to known frontend origins; never allow any origin in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? new[] { "http://localhost:3000" };

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


var app = builder.Build();

// Auto-migrate database and seed data (regions + demo tenant/user)
if (!isSwaggerGen)
{
    await app.SeedDataAsync(seedDemoData: app.Environment.IsDevelopment());
}

// Configure the HTTP request pipeline
// Static files must be enabled for Swagger UI
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlfTekPro HRMS API v1");
    options.RoutePrefix = "swagger";
});

// Hangfire dashboard — dev-only, localhost requests only
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
    });
}

// Rate limiting — must run before auth and controllers
app.UseIpRateLimiting();

// Global exception handler - returns consistent JSON ApiResponse for all errors
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors("AllowFrontend");

// Routing must be added before authentication
app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// CRITICAL: Tenant middleware MUST run AFTER UseAuthentication()
// It extracts tenant_id from JWT claims and sets it in TenantContext
// This enables automatic tenant isolation via EF Core Global Query Filters
app.UseTenantContext();

app.MapControllers();

// SWAGGER_GEN mode: generate openapi.json and exit without starting the server
if (isSwaggerGen)
{
    var outputPath = Environment.GetEnvironmentVariable("SWAGGER_OUTPUT") ?? "/tmp/swagger_new.yaml";
    var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
    var swagger = swaggerProvider.GetSwagger("v1");
    await using var fileStream = File.Create(outputPath);
    await using var sw = new StreamWriter(fileStream);
    swagger.SerializeAsV3(new OpenApiYamlWriter(sw));
    await sw.FlushAsync();
    Console.WriteLine($"Swagger generated at: {outputPath}");
    return;
}

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint — excluded from OpenAPI spec (not an API resource)
app.MapGet("/", () => new
{
    Application = "AlfTekPro Multi-Tenant HRMS",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Documentation = "/swagger"
}).ExcludeFromDescription();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
