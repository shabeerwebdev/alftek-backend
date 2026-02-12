# 🗄️ Database Setup Guide - Quick Start

This guide walks you through setting up the complete database schema with initial data.

---

## 🚀 **Option 1: All-in-One Setup (Recommended)**

Run the complete setup script that does everything:

```powershell
cd c:\Users\Admin\Documents\alftekpro\backend
.\setup-database.ps1
```

This single command will:
- ✅ Check prerequisites (.NET SDK, EF Core tools)
- ✅ Start PostgreSQL (if not running)
- ✅ Build the solution
- ✅ Create the `InitialCreate` migration
- ✅ Apply migration (create all 24 tables)
- ✅ Seed 3 regions (UAE, USA, India)

**Done!** Your database is ready to use.

---

## 📋 **Option 2: Step-by-Step Manual Setup**

### Step 1: Start PostgreSQL

```bash
docker-compose up -d postgres
```

### Step 2: Run Migration

```powershell
.\run-migration.ps1
```

### Step 3: Seed Regions

**Option A: Using SQL File**
```bash
docker exec -i alftekpro-postgres psql -U hrms_user -d alftekpro_hrms < seed-regions.sql
```

**Option B: Manual SQL**
```bash
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms
```

Then paste this SQL:
```sql
INSERT INTO regions (id, code, name, currency_code, date_format, direction, language_code, timezone, created_at)
VALUES
(gen_random_uuid(), 'UAE', 'United Arab Emirates', 'AED', 'dd/MM/yyyy', 'rtl', 'ar', 'Asia/Dubai', NOW()),
(gen_random_uuid(), 'USA', 'United States', 'USD', 'MM/dd/yyyy', 'ltr', 'en', 'America/New_York', NOW()),
(gen_random_uuid(), 'IND', 'India', 'INR', 'dd/MM/yyyy', 'ltr', 'hi', 'Asia/Kolkata', NOW());
```

---

## ✅ Verify Setup

### Check Tables Were Created

```bash
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms -c "\dt"
```

**Expected:** You should see 24 tables + `__EFMigrationsHistory`

### Check Regions Were Seeded

```bash
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms -c "SELECT code, name, currency_code, timezone FROM regions;"
```

**Expected Output:**
```
 code |          name           | currency_code |     timezone
------+-------------------------+---------------+------------------
 UAE  | United Arab Emirates    | AED           | Asia/Dubai
 USA  | United States           | USD           | America/New_York
 IND  | India                   | INR           | Asia/Kolkata
```

---

## 📊 What Was Created

### **24 Database Tables**

| Module | Tables |
|--------|--------|
| **Platform** (4) | regions, tenants, users, form_templates |
| **Core HR** (5) | departments, designations, locations, employees, employee_job_histories |
| **Workforce** (3) | shift_masters, employee_rosters, attendance_logs |
| **Leave** (3) | leave_types, leave_balances, leave_requests |
| **Workflow** (1) | user_tasks |
| **Payroll** (4) | salary_components, salary_structures, payroll_runs, payslips |
| **Assets** (2) | assets, asset_assignments |

### **3 Seeded Regions**

| Code | Name | Currency | Language | Direction | Timezone |
|------|------|----------|----------|-----------|----------|
| **UAE** | United Arab Emirates | AED | Arabic (ar) | RTL | Asia/Dubai |
| **USA** | United States | USD | English (en) | LTR | America/New_York |
| **IND** | India | INR | Hindi (hi) | LTR | Asia/Kolkata |

---

## 🔧 Troubleshooting

### Error: "Connection refused"

**Fix:**
```bash
docker-compose up -d postgres
docker-compose logs postgres
```

### Error: "Password authentication failed"

**Fix:** Check `appsettings.json` connection string:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=alftekpro_hrms;Username=hrms_user;Password=hrms_dev_pass_2024;"
```

### Error: "Build failed"

**Fix:**
```bash
dotnet restore
dotnet build
```

### Regions Not Seeded

**Fix:** Run the SQL seeder manually:
```bash
docker exec -i alftekpro-postgres psql -U hrms_user -d alftekpro_hrms < seed-regions.sql
```

---

## 🎯 Next Steps

After database setup is complete:

1. **✅ Database is ready** - All tables and regions created
2. **➡️ Implement Auth Controller** - Login, refresh, logout endpoints
3. **➡️ Implement Tenant Onboarding** - Registration endpoint
4. **➡️ Test API** - Verify database connectivity

---

## 📁 Related Files

- **[setup-database.ps1](setup-database.ps1)** - All-in-one setup script
- **[run-migration.ps1](run-migration.ps1)** - Migration only
- **[seed-regions.sql](seed-regions.sql)** - SQL seeder for regions
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Detailed migration docs
- **[QUICK_COMMANDS.md](QUICK_COMMANDS.md)** - Quick reference

---

## 💡 Tips

- The setup script is **idempotent** - safe to run multiple times
- Migrations are automatically detected if they already exist
- Region seeding checks for existing data before inserting
- All scripts include detailed logging and error messages

---

**Ready to set up the database?** Run:

```powershell
.\setup-database.ps1
```

That's it! 🎉
