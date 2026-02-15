using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Common;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.Infrastructure.Data.Seeding;
using AlfTekPro.API.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Environment.EnvironmentName = "Development";

// Add services to the container
builder.Services.AddControllers();
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

// Tenant Context (Scoped - one instance per HTTP request)
builder.Services.AddScoped<ITenantContext, TenantContext>();

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

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Health checks
builder.Services.AddHealthChecks();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Auto-migrate database and seed data (regions + demo tenant/user)
await app.SeedDataAsync(seedDemoData: app.Environment.IsDevelopment());

// Configure the HTTP request pipeline
// Static files must be enabled for Swagger UI
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlfTekPro HRMS API v1");
    options.RoutePrefix = "swagger";
});

// Global exception handler - returns consistent JSON ApiResponse for all errors
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors("AllowAll");

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

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new
{
    Application = "AlfTekPro Multi-Tenant HRMS",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Documentation = "/swagger"
});

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
