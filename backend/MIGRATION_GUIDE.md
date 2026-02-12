# 🗄️ Database Migration Guide

## Prerequisites

Before running migrations, ensure:

1. ✅ **.NET 8 SDK** is installed
2. ✅ **PostgreSQL** is running (via Docker or locally)
3. ✅ **EF Core Tools** are installed globally

### Install EF Core Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:
```bash
dotnet ef --version
```

---

## Option 1: Using the Migration Script (Recommended)

### Windows (PowerShell)

```powershell
cd backend
.\run-migration.ps1
```

### Linux/Mac (Bash)

```bash
cd backend
chmod +x run-migration.sh
./run-migration.sh
```

---

## Option 2: Manual Migration Commands

### Step 1: Ensure PostgreSQL is Running

```bash
# From project root
docker-compose up -d postgres

# Verify PostgreSQL is running
docker-compose ps
```

### Step 2: Create the Initial Migration

```bash
# From backend/ directory
dotnet ef migrations add InitialCreate \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API \
    --output-dir Data/Migrations
```

**What this does:**
- Analyzes all 24 entity configurations
- Generates migration files in `src/AlfTekPro.Infrastructure/Data/Migrations/`
- Creates:
  - `<timestamp>_InitialCreate.cs` - Migration Up/Down methods
  - `<timestamp>_InitialCreate.Designer.cs` - Metadata
  - `HrmsDbContextModelSnapshot.cs` - Current model snapshot

### Step 3: Review the Generated Migration

Open the generated migration file and verify:
- ✅ All 24 tables are being created
- ✅ JSONB columns are properly configured
- ✅ Indexes are created (tenant_id, unique constraints, etc.)
- ✅ Foreign keys with correct cascade/restrict behavior

### Step 4: Apply the Migration

```bash
dotnet ef database update \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API
```

**What this does:**
- Connects to PostgreSQL using connection string from `appsettings.json`
- Executes the migration SQL
- Creates all tables, indexes, and constraints
- Updates `__EFMigrationsHistory` table

---

## Verification

### Option A: Using Database Shell

```bash
# Connect to PostgreSQL
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms

# List all tables
\dt

# Check a specific table structure
\d employees

# Exit
\q
```

### Option B: Using pgAdmin

1. Navigate to `http://localhost:5050`
2. Login with credentials from `docker-compose.yml`
3. Connect to `alftekpro-postgres` server
4. Browse `alftekpro_hrms` database
5. Verify tables in `public` schema

### Expected Tables (24 total)

**Platform Module (4 tables):**
- `regions`
- `tenants`
- `users`
- `form_templates`

**Core HR Module (5 tables):**
- `departments`
- `designations`
- `locations`
- `employees`
- `employee_job_histories`

**Workforce Module (3 tables):**
- `shift_masters`
- `employee_rosters`
- `attendance_logs`

**Leave Module (3 tables):**
- `leave_types`
- `leave_balances`
- `leave_requests`

**Workflow Module (1 table):**
- `user_tasks`

**Payroll Module (4 tables):**
- `salary_components`
- `salary_structures`
- `payroll_runs`
- `payslips`

**Assets Module (2 tables):**
- `assets`
- `asset_assignments`

**System Tables (2 tables):**
- `__EFMigrationsHistory` - Tracks applied migrations
- `spatial_ref_sys` - PostGIS (if installed)

---

## Troubleshooting

### Error: "Connection refused" or "Could not connect to server"

**Solution:**
```bash
# Start PostgreSQL
docker-compose up -d postgres

# Check logs
docker-compose logs postgres
```

### Error: "Password authentication failed"

**Solution:**
Check connection string in `appsettings.json`:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=alftekpro_hrms;Username=hrms_user;Password=hrms_dev_pass_2024;"
```

### Error: "Build failed" or "Compilation errors"

**Solution:**
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Then retry migration
```

### Error: "Migration already exists"

**Solution:**
```bash
# List existing migrations
dotnet ef migrations list --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API

# Remove the last migration if needed
dotnet ef migrations remove --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API
```

### Error: "JSONB type not supported"

**Solution:**
Ensure you're using **PostgreSQL** (not SQL Server or MySQL). The project uses PostgreSQL-specific features (JSONB, GIN indexes).

---

## Useful Migration Commands

### List all migrations
```bash
dotnet ef migrations list \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API
```

### Generate SQL script (without applying)
```bash
dotnet ef migrations script \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API \
    --output migration.sql
```

### Remove last migration (if not applied)
```bash
dotnet ef migrations remove \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API
```

### Rollback to specific migration
```bash
dotnet ef database update <MigrationName> \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API
```

### Drop entire database (DANGEROUS!)
```bash
dotnet ef database drop \
    --project src/AlfTekPro.Infrastructure \
    --startup-project src/AlfTekPro.API
```

---

## Next Steps After Migration

1. ✅ **Seed Initial Data** - Run data seeder to populate regions (UAE, USA, India)
2. ✅ **Test Database Connection** - Verify API can connect to database
3. ✅ **Implement Controllers** - Start building API endpoints
4. ✅ **Write Tests** - Create integration tests for database operations

---

## Notes

- Migrations are stored in `src/AlfTekPro.Infrastructure/Data/Migrations/`
- Migration history is tracked in `__EFMigrationsHistory` table
- Always review generated migrations before applying to production
- Use migration scripts for production deployments (not direct `database update`)
- Keep migrations in version control (Git)

---

**Created:** 2026-02-11
**Last Updated:** 2026-02-11
**Status:** Ready for initial migration
