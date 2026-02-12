using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlfTekPro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    asset_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Available"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_parent_department_id",
                        column: x => x.parent_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "designations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_designations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_days_per_year = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_carry_forward = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    radius_meters = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    s3_path_pdf_bundle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    date_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "ltr"),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salary_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_components", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salary_structures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    components_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salary_structures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    grace_period_mins = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    total_hours = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_masters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "form_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    schema_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_templates_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    subscription_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subscription_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenants_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    joining_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dynamic_data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actioned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_tasks_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    returned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    returned_condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    return_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_assignments_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asset_assignments_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    clock_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clock_in_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    clock_in_latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    clock_in_longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    clock_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clock_out_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_late = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    late_by_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_regularized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    regularization_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_logs_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_job_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporting_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    salary_tier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    change_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_job_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_designation_id",
                        column: x => x.designation_id,
                        principalTable: "designations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_reporting_manager_id",
                        column: x => x.reporting_manager_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employee_job_histories_salary_tier_id",
                        column: x => x.salary_tier_id,
                        principalTable: "salary_structures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_rosters", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_rosters_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_rosters_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift_masters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    accrued = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    used = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_balances_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_leave_balances_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    days_count = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approver_comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_requests_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leave_requests_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_leave_requests_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payslips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    working_days = table.Column<int>(type: "integer", nullable: false),
                    present_days = table.Column<int>(type: "integer", nullable: false),
                    gross_earnings = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_deductions = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_pay = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    breakdown_json = table.Column<string>(type: "jsonb", nullable: false),
                    pdf_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payslips", x => x.id);
                    table.ForeignKey(
                        name: "fk_payslips_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payslips_payroll_run_id",
                        column: x => x.payroll_run_id,
                        principalTable: "payroll_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_assignments_asset_id",
                table: "asset_assignments",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_assignments_asset_id_returned_date",
                table: "asset_assignments",
                columns: new[] { "asset_id", "returned_date" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_assignments_employee_id",
                table: "asset_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_assignments_tenant_id",
                table: "asset_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id",
                table: "assets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_asset_code",
                table: "assets",
                columns: new[] { "tenant_id", "asset_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_status",
                table: "assets",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_employee_id",
                table: "attendance_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_employee_id_date",
                table: "attendance_logs",
                columns: new[] { "employee_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_employee_id_is_late",
                table: "attendance_logs",
                columns: new[] { "employee_id", "is_late" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_tenant_id",
                table: "attendance_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_tenant_id_date",
                table: "attendance_logs",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_tenant_id_status",
                table: "attendance_logs",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_departments_parent_department_id",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_tenant_id",
                table: "departments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_tenant_id_name",
                table: "departments",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_designations_tenant_id",
                table: "designations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_tenant_id_level",
                table: "designations",
                columns: new[] { "tenant_id", "level" });

            migrationBuilder.CreateIndex(
                name: "ix_designations_tenant_id_title",
                table: "designations",
                columns: new[] { "tenant_id", "title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_job_histories_created_by",
                table: "employee_job_histories",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_department_id",
                table: "employee_job_histories",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_designation_id",
                table: "employee_job_histories",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_employee_id",
                table: "employee_job_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_employee_id_valid_from_valid_to",
                table: "employee_job_histories",
                columns: new[] { "employee_id", "valid_from", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_employee_id_valid_to",
                table: "employee_job_histories",
                columns: new[] { "employee_id", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_location_id",
                table: "employee_job_histories",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_reporting_manager_id",
                table: "employee_job_histories",
                column: "reporting_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_job_histories_salary_tier_id",
                table: "employee_job_histories",
                column: "salary_tier_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_job_histories_tenant_id",
                table: "employee_job_histories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_rosters_employee_id",
                table: "employee_rosters",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_rosters_employee_id_effective_date",
                table: "employee_rosters",
                columns: new[] { "employee_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_rosters_shift_id",
                table: "employee_rosters",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_rosters_tenant_id",
                table: "employee_rosters",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_dynamic_data_gin",
                table: "employees",
                column: "dynamic_data",
                filter: "dynamic_data IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id",
                table: "employees",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_email",
                table: "employees",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_employee_code",
                table: "employees",
                columns: new[] { "tenant_id", "employee_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_status",
                table: "employees",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_user_id",
                table: "employees",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_is_active",
                table: "form_templates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_region_id",
                table: "form_templates",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_region_id_module",
                table: "form_templates",
                columns: new[] { "region_id", "module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_schema_json_gin",
                table: "form_templates",
                column: "schema_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_employee_id",
                table: "leave_balances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_employee_id_leave_type_id_year",
                table: "leave_balances",
                columns: new[] { "employee_id", "leave_type_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_leave_type_id",
                table: "leave_balances",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_tenant_id",
                table: "leave_balances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_approved_by",
                table: "leave_requests",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_employee_id",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_employee_id_dates",
                table: "leave_requests",
                columns: new[] { "employee_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_leave_type_id",
                table: "leave_requests",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id",
                table: "leave_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id_status",
                table: "leave_requests",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_tenant_id",
                table: "leave_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_tenant_id_code",
                table: "leave_types",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_id",
                table: "locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_id_name",
                table: "locations",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id",
                table: "payroll_runs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_runs_tenant_id_month_year",
                table: "payroll_runs",
                columns: new[] { "tenant_id", "month", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payslips_breakdown_json_gin",
                table: "payslips",
                column: "breakdown_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_payslips_employee_id",
                table: "payslips",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payslips_payroll_run_id_employee_id",
                table: "payslips",
                columns: new[] { "payroll_run_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payslips_tenant_id",
                table: "payslips",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_regions_code",
                table: "regions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_tenant_id",
                table: "salary_components",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_salary_components_tenant_id_code",
                table: "salary_components",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_salary_structures_components_json_gin",
                table: "salary_structures",
                column: "components_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structures_tenant_id",
                table: "salary_structures",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_salary_structures_tenant_id_name",
                table: "salary_structures",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_masters_tenant_id",
                table: "shift_masters",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_masters_tenant_id_code",
                table: "shift_masters",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_masters_tenant_id_is_active",
                table: "shift_masters",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_is_active",
                table: "tenants",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_region_id",
                table: "tenants",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_subdomain",
                table: "tenants",
                column: "subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tasks_owner_user_id",
                table: "user_tasks",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_tasks_owner_user_id_status",
                table: "user_tasks",
                columns: new[] { "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_user_tasks_tenant_id",
                table: "user_tasks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_tasks_tenant_id_entity",
                table: "user_tasks",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                table: "users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_assignments");

            migrationBuilder.DropTable(
                name: "attendance_logs");

            migrationBuilder.DropTable(
                name: "employee_job_histories");

            migrationBuilder.DropTable(
                name: "employee_rosters");

            migrationBuilder.DropTable(
                name: "form_templates");

            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "payslips");

            migrationBuilder.DropTable(
                name: "salary_components");

            migrationBuilder.DropTable(
                name: "user_tasks");

            migrationBuilder.DropTable(
                name: "assets");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "designations");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "salary_structures");

            migrationBuilder.DropTable(
                name: "shift_masters");

            migrationBuilder.DropTable(
                name: "leave_types");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "payroll_runs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "regions");
        }
    }
}
