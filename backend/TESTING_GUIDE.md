# 🧪 Core HR Testing Guide

Complete testing guide for all Core HR modules.

---

## Prerequisites

✅ PostgreSQL running (`docker-compose up -d postgres`)
✅ Database migrated and seeded (3 regions)
✅ API running (`dotnet run --project src/AlfTekPro.API`)

---

## Quick Test (Automated)

### Option 1: PowerShell (Recommended for Windows)

```powershell
cd c:\Users\Admin\Documents\alftekpro\backend
.\test-core-hr.ps1
```

**Tests:**
- ✅ Regions endpoint (public)
- ✅ Tenant onboarding
- ✅ Authentication (login)
- ✅ Departments CRUD + hierarchy
- ✅ Designations CRUD
- ✅ Locations CRUD + geofencing
- ✅ Employees CRUD + JSONB dynamic data
- ✅ Filtering and updates

### Option 2: Verify Database

```powershell
.\verify-database.ps1
```

**Checks:**
- Table counts
- Tenant isolation
- JSONB data structure
- Department hierarchy
- Geofencing data

---

## Manual Testing (Swagger)

1. **Start API:**
   ```powershell
   dotnet run --project src\AlfTekPro.API
   ```

2. **Open Swagger:**
   http://localhost:5000/swagger

3. **Follow test sequence:**

### Step 1: Get Regions
**GET /api/regions** (no auth needed)

Expected: 3 regions (UAE, USA, IND)

Copy UAE region ID for next step.

---

### Step 2: Onboard Tenant
**POST /api/tenants/onboard**

```json
{
  "organizationName": "Test Corp",
  "subdomain": "testcorp",
  "regionId": "<paste-uae-region-id>",
  "adminFirstName": "Admin",
  "adminLastName": "User",
  "adminEmail": "admin@testcorp.com",
  "adminPassword": "Admin@123",
  "contactPhone": "+971501234567"
}
```

Expected: 201 Created with tenant and admin user details

---

### Step 3: Login
**POST /api/auth/login**

```json
{
  "email": "admin@testcorp.com",
  "password": "Admin@123"
}
```

Expected: JWT token with `tenant_id` claim

**Authorize Swagger:**
1. Click "Authorize" button 🔒
2. Paste token
3. Click "Authorize"

---

### Step 4: Create Department
**POST /api/departments**

```json
{
  "name": "Engineering",
  "code": "ENG",
  "description": "Engineering Department",
  "isActive": true
}
```

**Create Sub-Department:**
```json
{
  "name": "Backend Team",
  "code": "ENG-BACKEND",
  "parentDepartmentId": "<paste-engineering-id>",
  "isActive": true
}
```

**Test Hierarchy:**
GET /api/departments/hierarchy

Expected: Backend Team nested under Engineering

---

### Step 5: Create Designation
**POST /api/designations**

```json
{
  "title": "Senior Software Engineer",
  "code": "SSE",
  "level": 3,
  "description": "5+ years experience",
  "isActive": true
}
```

---

### Step 6: Create Location
**POST /api/locations**

```json
{
  "name": "Dubai Head Office",
  "code": "DXB-HQ",
  "address": "Sheikh Zayed Road",
  "city": "Dubai",
  "country": "United Arab Emirates",
  "latitude": 25.2048,
  "longitude": 55.2708,
  "radiusMeters": 100,
  "contactPhone": "+971501234567",
  "isActive": true
}
```

Expected: `hasGeofence: true`

---

### Step 7: Create Employee with JSONB
**POST /api/employees**

```json
{
  "employeeCode": "EMP001",
  "firstName": "Ahmed",
  "lastName": "Al Maktoum",
  "email": "ahmed@testcorp.com",
  "phone": "+971501234567",
  "dateOfBirth": "1990-01-15",
  "gender": "Male",
  "joiningDate": "2024-01-01",
  "departmentId": "<paste-dept-id>",
  "designationId": "<paste-designation-id>",
  "locationId": "<paste-location-id>",
  "status": "Active",
  "dynamicData": {
    "emirates_id": "784-1990-1234567-1",
    "passport_number": "A1234567",
    "visa_expiry": "2025-12-31"
  }
}
```

Expected:
- ✅ Calculated `age`
- ✅ Calculated `tenureDays`
- ✅ `dynamicData` preserved

---

## Verification Checklist

### ✅ Tenant Isolation
1. Onboard second tenant (e.g., "techcorp")
2. Login as techcorp admin
3. GET /api/employees

**Expected:** No employees from testcorp visible

### ✅ JSONB Dynamic Data
1. GET /api/employees/code/EMP001
2. Check `dynamicData` field

**Expected:** All custom fields returned

### ✅ Department Hierarchy
1. GET /api/departments/hierarchy

**Expected:** Tree structure with children nested

### ✅ Geofencing
1. GET /api/locations/{id}

**Expected:** `hasGeofence: true`, latitude/longitude/radius populated

### ✅ Role-Based Access
1. Logout
2. Try creating department without token

**Expected:** 401 Unauthorized

### ✅ Filtering
- GET /api/employees?status=Active
- GET /api/employees?departmentId={id}
- GET /api/employees?designationId={id}

**Expected:** Filtered results

---

## Database Validation

```bash
docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms

-- Check JSONB data
SELECT employee_code, dynamic_data::jsonb
FROM employees
WHERE dynamic_data IS NOT NULL;

-- Check hierarchy
SELECT d1.name as parent, d2.name as child
FROM departments d1
INNER JOIN departments d2 ON d1.id = d2.parent_department_id;

-- Check tenant isolation
SELECT tenant_id, COUNT(*)
FROM employees
GROUP BY tenant_id;

-- Check geofencing
SELECT name, latitude, longitude, radius_meters
FROM locations
WHERE latitude IS NOT NULL;
```

---

## Success Criteria

✅ All endpoints return expected status codes
✅ Tenant isolation works (cross-tenant data hidden)
✅ JSONB dynamic data stored and retrieved correctly
✅ Department hierarchy displays properly
✅ Geofencing coordinates preserved
✅ Age and tenure calculated automatically
✅ Role-based authorization enforced
✅ Filters work correctly
✅ Updates modify data successfully

---

## Troubleshooting

**Error: "Connection refused"**
- Start API: `dotnet run --project src/AlfTekPro.API`

**Error: "401 Unauthorized"**
- Get new token from login endpoint
- Click "Authorize" in Swagger

**Error: "Department not found"**
- Create department first
- Use correct department ID

**Error: "JSONB parse error"**
- Ensure `dynamicData` is valid JSON object
- Keys and values must be quoted properly

---

**Ready to test!** Run `.\test-core-hr.ps1` for automated testing.
