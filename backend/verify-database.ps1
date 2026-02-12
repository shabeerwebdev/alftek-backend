#!/usr/bin/env pwsh
# Database Verification Script
# Checks PostgreSQL database for test data

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Database Verification" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

$containerName = "alftekpro-postgres"
$dbName = "alftekpro_hrms"
$dbUser = "hrms_user"

function Run-PostgresQuery {
    param([string]$Query, [string]$Description)

    Write-Host "Checking: $Description" -ForegroundColor Yellow
    try {
        $result = docker exec -i $containerName psql -U $dbUser -d $dbName -c $Query 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host $result -ForegroundColor Gray
            Write-Host "  ✅ Success" -ForegroundColor Green
        } else {
            Write-Host $result -ForegroundColor Red
            Write-Host "  ❌ Failed" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "[1] Regions Count" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT COUNT(*) as total, string_agg(code, ', ') as codes FROM regions;" `
    -Description "Total regions and their codes"

Write-Host "[2] Tenants" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT id, name, subdomain, is_active FROM tenants LIMIT 5;" `
    -Description "List of tenants"

Write-Host "[3] Users" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT id, email, role, tenant_id IS NOT NULL as has_tenant FROM users LIMIT 5;" `
    -Description "List of users"

Write-Host "[4] Refresh Tokens" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT COUNT(*) as total_tokens, COUNT(*) FILTER (WHERE revoked_at IS NULL) as active_tokens FROM refresh_tokens;" `
    -Description "Token statistics"

Write-Host "[5] Departments" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT id, name, code, parent_department_id IS NOT NULL as has_parent FROM departments LIMIT 10;" `
    -Description "List of departments"

Write-Host "[6] Designations" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT id, title, code, level FROM designations ORDER BY level LIMIT 10;" `
    -Description "List of designations"

Write-Host "[7] Locations (with Geofencing)" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT name, code, latitude IS NOT NULL as has_geofence FROM locations LIMIT 10;" `
    -Description "Locations with geofencing status"

Write-Host "[8] Employees" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT employee_code, first_name, last_name, status FROM employees LIMIT 10;" `
    -Description "List of employees"

Write-Host "[9] Employee Dynamic Data (JSONB)" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT employee_code, dynamic_data::jsonb FROM employees WHERE dynamic_data IS NOT NULL LIMIT 3;" `
    -Description "Employee JSONB dynamic data"

Write-Host "[10] Tenant Isolation Check" -ForegroundColor Cyan
Run-PostgresQuery -Query "SELECT tenant_id, COUNT(*) as employee_count FROM employees GROUP BY tenant_id;" `
    -Description "Employees grouped by tenant"

Write-Host "[11] Department Hierarchy" -ForegroundColor Cyan
Run-PostgresQuery -Query @"
WITH RECURSIVE dept_tree AS (
    SELECT id, name, parent_department_id, name as path, 0 as level
    FROM departments
    WHERE parent_department_id IS NULL
    UNION ALL
    SELECT d.id, d.name, d.parent_department_id, dt.path || ' > ' || d.name, dt.level + 1
    FROM departments d
    INNER JOIN dept_tree dt ON d.parent_department_id = dt.id
)
SELECT level, path FROM dept_tree ORDER BY path LIMIT 10;
"@ -Description "Department hierarchy tree"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Verification Complete" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
