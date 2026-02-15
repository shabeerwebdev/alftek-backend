# 🚀 Quick Migration Commands

Copy and paste these commands in your terminal.

## 1️⃣ Start PostgreSQL

```bash
docker-compose up -d postgres
```

## 2️⃣ Install EF Core Tools (if needed)

```bash
dotnet tool install --global dotnet-ef
```

## 3️⃣ Create Migration

```bash
# Using Docker (from root directory)
docker exec -it alftekpro-api dotnet ef migrations add MigrationName --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API --output-dir Data/Migrations

# Local (from backend/ directory)
dotnet ef migrations add MigrationName --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API --output-dir Data/Migrations
```

## 4️⃣ Apply Migration

```bash
# Using Docker (from root directory)
docker exec -it alftekpro-api dotnet ef database update

# Local (from backend/ directory)
dotnet ef database update --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API
```

## 5️⃣ Verify Tables Created

```bash
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms -c "\dt"
```

---

## ✅ Expected Result

You should see 24 tables:
- regions, tenants, users, form_templates
- departments, designations, locations, employees, employee_job_histories
- shift_masters, employee_rosters, attendance_logs
- leave_types, leave_balances, leave_requests
- user_tasks
- salary_components, salary_structures, payroll_runs, payslips
- assets, asset_assignments

Plus `__EFMigrationsHistory` for tracking migrations.

---

## 🔧 Troubleshooting

**PostgreSQL not running?**
```bash
docker-compose up -d postgres && docker-compose logs -f postgres
```

**Connection failed?**
Check `appsettings.json` connection string matches Docker credentials.

**Build errors?**
```bash
dotnet restore && dotnet build
```

---

## 📝 One-Liner (PowerShell)

```powershell
.\run-migration.ps1
```

## 📝 One-Liner (Bash/Linux/Mac)

```bash
./run-migration.sh
```
