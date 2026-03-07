-- ==================================================
-- MyHRMS - Comprehensive Demo Seed Data
-- Organisation: Demo Company (UAE-based)
--
-- Fixed UUIDs must match DataSeeder.cs constants:
--   DemoTenantId   = 10000000-0000-4000-8000-000000000001
--   DemoSaUserId   = 10000000-0000-4000-8000-000000000002  (seeded by C# DataSeeder)
--   DemoAdminUserId= 10000000-0000-4000-8000-000000000003  (seeded by C# DataSeeder)
--   DemoMgrUserId  = 10000000-0000-4000-8000-000000000004  (seeded by C# DataSeeder)
--   DemoAdminEmpId = 10000000-0000-4000-8000-000000000005  (seeded by C# DataSeeder)
--
-- Login credentials:
--   SA       : sa@myhrms.com            / Admin@123  (seeded by C# DataSeeder)
--   Admin    : admin@demo.myhrms.com    / Demo@123   (seeded by C# DataSeeder)
--   Manager  : manager@demo.myhrms.com  / Demo@123   (seeded by C# DataSeeder)
--   Payroll  : payroll@demo.myhrms.com  / Demo@123
--   Employees: <name>@demo.myhrms.com   / Demo@123
-- ==================================================

\set ON_ERROR_STOP on

DO $$
DECLARE
    -- Fixed IDs (must match DataSeeder.cs)
    v_tenant_id  UUID := '10000000-0000-4000-8000-000000000001';
    v_admin_user UUID := '10000000-0000-4000-8000-000000000003';
    v_mgr_user   UUID := '10000000-0000-4000-8000-000000000004';
    v_emp_admin  UUID := '10000000-0000-4000-8000-000000000005';

    -- New user IDs
    v_pa_user    UUID := gen_random_uuid();
    v_emp1_user  UUID := gen_random_uuid();
    v_emp2_user  UUID := gen_random_uuid();
    v_emp3_user  UUID := gen_random_uuid();
    v_emp4_user  UUID := gen_random_uuid();
    v_emp5_user  UUID := gen_random_uuid();
    v_emp6_user  UUID := gen_random_uuid();
    v_emp7_user  UUID := gen_random_uuid();
    v_emp8_user  UUID := gen_random_uuid();

    -- Department / Designation / Location / Leave IDs (looked up from DB)
    v_dept_eng   UUID;
    v_dept_hr    UUID;
    v_dept_fin   UUID;
    v_dept_sales UUID;

    v_desig_swe      UUID;
    v_desig_srswe    UUID;
    v_desig_mgr      UUID;
    v_desig_analyst  UUID;
    v_desig_hr_mgr   UUID;
    v_desig_fin_mgr  UUID;
    v_desig_sales_ex UUID;

    v_loc_dxb UUID;
    v_loc_auh UUID;

    v_lt_annual UUID;
    v_lt_sick   UUID;
    v_lt_casual UUID;

    -- Salary structure / component IDs
    v_ss_eng  UUID := gen_random_uuid();
    v_ss_mgr  UUID := gen_random_uuid();
    v_ss_std  UUID := gen_random_uuid();

    v_sc_basic     UUID := gen_random_uuid();
    v_sc_hra       UUID := gen_random_uuid();
    v_sc_transport UUID := gen_random_uuid();
    v_sc_bonus     UUID := gen_random_uuid();
    v_sc_tax       UUID := gen_random_uuid();
    v_sc_insurance UUID := gen_random_uuid();
    v_sc_pf        UUID := gen_random_uuid();
    v_sc_adv       UUID := gen_random_uuid();

    -- Employee IDs
    v_emp_mgr UUID := gen_random_uuid();
    v_emp_pa  UUID := gen_random_uuid();
    v_emp_e1  UUID := gen_random_uuid();
    v_emp_e2  UUID := gen_random_uuid();
    v_emp_e3  UUID := gen_random_uuid();
    v_emp_e4  UUID := gen_random_uuid();
    v_emp_e5  UUID := gen_random_uuid();
    v_emp_e6  UUID := gen_random_uuid();
    v_emp_e7  UUID := gen_random_uuid();
    v_emp_e8  UUID := gen_random_uuid();

    -- Payroll run IDs (5 months)
    v_pr_oct UUID := gen_random_uuid();
    v_pr_nov UUID := gen_random_uuid();
    v_pr_dec UUID := gen_random_uuid();
    v_pr_jan UUID := gen_random_uuid();
    v_pr_feb UUID := gen_random_uuid();

    -- Shift ID
    v_shift_std UUID := gen_random_uuid();

    -- Asset IDs
    v_asset1 UUID := gen_random_uuid();
    v_asset2 UUID := gen_random_uuid();
    v_asset3 UUID := gen_random_uuid();
    v_asset4 UUID := gen_random_uuid();
    v_asset5 UUID := gen_random_uuid();
    v_asset6 UUID := gen_random_uuid();
    v_asset7 UUID := gen_random_uuid();

    -- BCrypt hash for "Demo@123" (deterministic, salt fixed, verified correct)
    v_pw TEXT := '$2a$11$rBFBIhDSBvv/G8DaRqCUPeQqT160jb9LnXdyPy21vLza70hv5KBdO';

BEGIN

-- ============================================================
-- Guard: skip if already seeded
-- ============================================================
IF EXISTS (SELECT 1 FROM salary_components WHERE tenant_id = v_tenant_id LIMIT 1) THEN
    RAISE NOTICE 'Demo seed data already present — skipping.';
    RETURN;
END IF;

-- ============================================================
-- 0. Look up IDs seeded by C# DataSeeder
-- ============================================================
SELECT id INTO v_dept_eng   FROM departments WHERE tenant_id = v_tenant_id AND code = 'ENG';
SELECT id INTO v_dept_hr    FROM departments WHERE tenant_id = v_tenant_id AND code = 'HR';
SELECT id INTO v_dept_fin   FROM departments WHERE tenant_id = v_tenant_id AND code = 'FIN';
SELECT id INTO v_dept_sales FROM departments WHERE tenant_id = v_tenant_id AND code = 'SALES';

SELECT id INTO v_desig_swe     FROM designations WHERE tenant_id = v_tenant_id AND code = 'SWE';
SELECT id INTO v_desig_srswe   FROM designations WHERE tenant_id = v_tenant_id AND code = 'SR-SWE';
SELECT id INTO v_desig_mgr     FROM designations WHERE tenant_id = v_tenant_id AND code = 'MGR';
SELECT id INTO v_desig_analyst FROM designations WHERE tenant_id = v_tenant_id AND code = 'ANL';

SELECT id INTO v_loc_dxb FROM locations WHERE tenant_id = v_tenant_id AND code = 'DXB-HQ';
SELECT id INTO v_loc_auh FROM locations WHERE tenant_id = v_tenant_id AND code = 'AUH';

SELECT id INTO v_lt_annual FROM leave_types WHERE tenant_id = v_tenant_id AND code = 'AL';
SELECT id INTO v_lt_sick   FROM leave_types WHERE tenant_id = v_tenant_id AND code = 'SL';
SELECT id INTO v_lt_casual FROM leave_types WHERE tenant_id = v_tenant_id AND code = 'CL';

-- ============================================================
-- 1. Extra designations
-- ============================================================
v_desig_hr_mgr   := gen_random_uuid();
v_desig_fin_mgr  := gen_random_uuid();
v_desig_sales_ex := gen_random_uuid();

INSERT INTO designations (id, tenant_id, title, code, level, created_at)
VALUES
    (v_desig_hr_mgr,   v_tenant_id, 'HR Manager',      'HR-MGR',    5, NOW()),
    (v_desig_fin_mgr,  v_tenant_id, 'Finance Manager', 'FIN-MGR',   5, NOW()),
    (v_desig_sales_ex, v_tenant_id, 'Sales Executive', 'SALES-EXC', 3, NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 2. Update admin employee (EMP001)
-- ============================================================
UPDATE employees
SET department_id  = v_dept_eng,
    designation_id = v_desig_mgr,
    location_id    = v_loc_dxb,
    gender         = 'Male',
    phone          = '+971-50-1001001',
    date_of_birth  = '1985-03-15',
    dynamic_data   = '{"emirates_id":"784-1985-1234567-1","passport_number":"A1234567","blood_group":"O+","visa_expiry":"2027-03-31"}'
WHERE id = v_emp_admin;

-- ============================================================
-- 3. Shift Master
-- ============================================================
INSERT INTO shift_masters (id, tenant_id, name, code, start_time, end_time, total_hours, is_active, created_at)
VALUES (v_shift_std, v_tenant_id, 'Standard Shift', 'STD', '09:00:00', '18:00:00', 9.0, true, NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 4. Salary Components
-- ============================================================
INSERT INTO salary_components (id, tenant_id, name, code, type, is_taxable, is_active, created_at)
VALUES
    (v_sc_basic,     v_tenant_id, 'Basic Salary',         'BASIC',     'Earning',   true,  true,  NOW()),
    (v_sc_hra,       v_tenant_id, 'House Rent Allowance', 'HRA',       'Earning',   false, true,  NOW()),
    (v_sc_transport, v_tenant_id, 'Transport Allowance',  'TRANSPORT', 'Earning',   false, true,  NOW()),
    (v_sc_bonus,     v_tenant_id, 'Performance Bonus',    'BONUS',     'Earning',   true,  true,  NOW()),
    (v_sc_tax,       v_tenant_id, 'Income Tax',           'TAX',       'Deduction', false, true,  NOW()),
    (v_sc_insurance, v_tenant_id, 'Medical Insurance',    'INSURANCE', 'Deduction', false, true,  NOW()),
    (v_sc_pf,        v_tenant_id, 'Provident Fund',       'PF',        'Deduction', false, true,  NOW()),
    (v_sc_adv,       v_tenant_id, 'Salary Advance',       'ADV',       'Deduction', false, false, NOW())
ON CONFLICT (tenant_id, code) DO NOTHING;

-- ============================================================
-- 5. Salary Structures  *** FIXED: cast string concatenation to ::jsonb ***
-- ============================================================
INSERT INTO salary_structures (id, tenant_id, name, components_json, created_at)
VALUES
    (v_ss_mgr, v_tenant_id, 'Management Band',
     ('[{"ComponentId":"' || v_sc_basic || '","Amount":25000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_hra || '","Amount":8000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_transport || '","Amount":2000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_tax || '","Amount":2500,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_insurance || '","Amount":800,"CalculationType":"Fixed"}]')::jsonb,
     NOW()),
    (v_ss_eng, v_tenant_id, 'Engineering Band',
     ('[{"ComponentId":"' || v_sc_basic || '","Amount":15000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_hra || '","Amount":5000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_transport || '","Amount":1500,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_tax || '","Amount":1200,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_insurance || '","Amount":500,"CalculationType":"Fixed"}]')::jsonb,
     NOW()),
    (v_ss_std, v_tenant_id, 'Standard Band',
     ('[{"ComponentId":"' || v_sc_basic || '","Amount":10000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_hra || '","Amount":3000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_transport || '","Amount":1000,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_tax || '","Amount":800,"CalculationType":"Fixed"},{"ComponentId":"' || v_sc_insurance || '","Amount":400,"CalculationType":"Fixed"}]')::jsonb,
     NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 6. New Users (Payroll Admin + 8 Employees)
-- ============================================================
INSERT INTO users (id, tenant_id, email, first_name, last_name, password_hash, role, is_active, created_at)
VALUES
    (v_pa_user,   v_tenant_id, 'payroll@demo.myhrms.com',     'Priya', 'Sharma', v_pw, 'PA',  true, NOW()),
    (v_emp1_user, v_tenant_id, 'ali.hassan@demo.myhrms.com',  'Ali',   'Hassan', v_pw, 'EMP', true, NOW()),
    (v_emp2_user, v_tenant_id, 'sara.ahmed@demo.myhrms.com',  'Sara',  'Ahmed',  v_pw, 'EMP', true, NOW()),
    (v_emp3_user, v_tenant_id, 'ravi.kumar@demo.myhrms.com',  'Ravi',  'Kumar',  v_pw, 'EMP', true, NOW()),
    (v_emp4_user, v_tenant_id, 'fatima.ali@demo.myhrms.com',  'Fatima','Ali',    v_pw, 'EMP', true, NOW()),
    (v_emp5_user, v_tenant_id, 'james.wilson@demo.myhrms.com','James', 'Wilson', v_pw, 'EMP', true, NOW()),
    (v_emp6_user, v_tenant_id, 'nour.khalid@demo.myhrms.com', 'Nour',  'Khalid', v_pw, 'EMP', true, NOW()),
    (v_emp7_user, v_tenant_id, 'chen.wei@demo.myhrms.com',    'Chen',  'Wei',    v_pw, 'EMP', true, NOW()),
    (v_emp8_user, v_tenant_id, 'layla.omar@demo.myhrms.com',  'Layla', 'Omar',   v_pw, 'EMP', true, NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 7. Employee Records (EMP002–EMP011)
-- ============================================================
INSERT INTO employees (id, tenant_id, user_id, employee_code, first_name, last_name, email, phone, date_of_birth, joining_date, status, department_id, designation_id, location_id, reporting_manager_id, gender, dynamic_data, created_at)
VALUES
    (v_emp_mgr, v_tenant_id, v_mgr_user,   'EMP002', 'Demo',   'Manager', 'manager@demo.myhrms.com',     '+971-50-1001002', '1982-07-20', '2020-01-15', 'Active', v_dept_eng,   v_desig_mgr,     v_loc_dxb, v_emp_admin, 'Male',   '{"emirates_id":"784-1982-2345678-2","passport_number":"B2345678","blood_group":"A+","visa_expiry":"2026-07-31"}', NOW()),
    (v_emp_pa,  v_tenant_id, v_pa_user,    'EMP003', 'Priya',  'Sharma',  'payroll@demo.myhrms.com',     '+971-50-1001003', '1990-11-05', '2021-03-01', 'Active', v_dept_fin,   v_desig_fin_mgr, v_loc_dxb, v_emp_admin, 'Female', '{"emirates_id":"784-1990-3456789-3","passport_number":"C3456789","blood_group":"B+","visa_expiry":"2026-11-30"}', NOW()),
    (v_emp_e1,  v_tenant_id, v_emp1_user,  'EMP004', 'Ali',    'Hassan',  'ali.hassan@demo.myhrms.com',  '+971-50-1001004', '1995-04-12', '2022-06-01', 'Active', v_dept_eng,   v_desig_swe,     v_loc_dxb, v_emp_mgr,   'Male',   '{"emirates_id":"784-1995-4567890-4","passport_number":"D4567890","blood_group":"AB+","visa_expiry":"2027-04-30"}', NOW()),
    (v_emp_e2,  v_tenant_id, v_emp2_user,  'EMP005', 'Sara',   'Ahmed',   'sara.ahmed@demo.myhrms.com',  '+971-50-1001005', '1993-08-25', '2021-09-15', 'Active', v_dept_hr,    v_desig_hr_mgr,  v_loc_dxb, v_emp_admin, 'Female', '{"emirates_id":"784-1993-5678901-5","passport_number":"E5678901","blood_group":"O-","visa_expiry":"2026-08-31"}', NOW()),
    (v_emp_e3,  v_tenant_id, v_emp3_user,  'EMP006', 'Ravi',   'Kumar',   'ravi.kumar@demo.myhrms.com',  '+971-50-1001006', '1991-01-30', '2020-11-01', 'Active', v_dept_eng,   v_desig_srswe,   v_loc_dxb, v_emp_mgr,   'Male',   '{"emirates_id":"784-1991-6789012-6","passport_number":"F6789012","blood_group":"A-","visa_expiry":"2027-01-31"}', NOW()),
    (v_emp_e4,  v_tenant_id, v_emp4_user,  'EMP007', 'Fatima', 'Ali',     'fatima.ali@demo.myhrms.com',  '+971-50-1001007', '1997-06-18', '2023-02-01', 'Active', v_dept_sales, v_desig_sales_ex,v_loc_auh, v_emp_admin, 'Female', '{"emirates_id":"784-1997-7890123-7","passport_number":"G7890123","blood_group":"B-","visa_expiry":"2026-06-30"}', NOW()),
    (v_emp_e5,  v_tenant_id, v_emp5_user,  'EMP008', 'James',  'Wilson',  'james.wilson@demo.myhrms.com','+971-50-1001008', '1988-09-10', '2019-07-01', 'Active', v_dept_eng,   v_desig_srswe,   v_loc_dxb, v_emp_mgr,   'Male',   '{"emirates_id":"784-1988-8901234-8","passport_number":"H8901234","blood_group":"O+","visa_expiry":"2027-09-30"}', NOW()),
    (v_emp_e6,  v_tenant_id, v_emp6_user,  'EMP009', 'Nour',   'Khalid',  'nour.khalid@demo.myhrms.com', '+971-50-1001009', '1996-03-22', '2022-10-01', 'Active', v_dept_fin,   v_desig_analyst, v_loc_dxb, v_emp_pa,    'Female', '{"emirates_id":"784-1996-9012345-9","passport_number":"I9012345","blood_group":"AB-","visa_expiry":"2026-03-31"}', NOW()),
    (v_emp_e7,  v_tenant_id, v_emp7_user,  'EMP010', 'Chen',   'Wei',     'chen.wei@demo.myhrms.com',    '+971-50-1001010', '1994-12-08', '2021-05-15', 'Active', v_dept_eng,   v_desig_swe,     v_loc_dxb, v_emp_mgr,   'Male',   '{"emirates_id":"784-1994-0123456-0","passport_number":"J0123456","blood_group":"A+","visa_expiry":"2027-12-31"}', NOW()),
    (v_emp_e8,  v_tenant_id, v_emp8_user,  'EMP011', 'Layla',  'Omar',    'layla.omar@demo.myhrms.com',  '+971-50-1001011', '1999-05-14', '2023-08-01', 'Active', v_dept_sales, v_desig_sales_ex,v_loc_auh, v_emp_admin, 'Female', '{"emirates_id":"784-1999-1234560-1","passport_number":"K1234560","blood_group":"O+","visa_expiry":"2026-05-31"}', NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 8. Employee Job Histories
-- ============================================================
INSERT INTO employee_job_histories (id, tenant_id, employee_id, department_id, designation_id, location_id, salary_tier_id, reporting_manager_id, valid_from, valid_to, change_type, change_reason, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_admin, v_dept_eng,   v_desig_mgr,     v_loc_dxb, v_ss_mgr, NULL,        '2019-01-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr,   v_dept_eng,   v_desig_mgr,     v_loc_dxb, v_ss_mgr, v_emp_admin, '2020-01-15', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_pa,    v_dept_fin,   v_desig_fin_mgr, v_loc_dxb, v_ss_mgr, v_emp_admin, '2021-03-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e1,    v_dept_eng,   v_desig_swe,     v_loc_dxb, v_ss_eng, v_emp_mgr,   '2022-06-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,    v_dept_hr,    v_desig_hr_mgr,  v_loc_dxb, v_ss_mgr, v_emp_admin, '2021-09-15', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,    v_dept_eng,   v_desig_srswe,   v_loc_dxb, v_ss_eng, v_emp_mgr,   '2020-11-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e4,    v_dept_sales, v_desig_sales_ex,v_loc_auh, v_ss_std, v_emp_admin, '2023-02-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,    v_dept_eng,   v_desig_srswe,   v_loc_dxb, v_ss_eng, v_emp_mgr,   '2019-07-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e6,    v_dept_fin,   v_desig_analyst, v_loc_dxb, v_ss_std, v_emp_pa,    '2022-10-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,    v_dept_eng,   v_desig_swe,     v_loc_dxb, v_ss_eng, v_emp_mgr,   '2021-05-15', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e8,    v_dept_sales, v_desig_sales_ex,v_loc_auh, v_ss_std, v_emp_admin, '2023-08-01', NULL, 'Hire',      'Initial hire',        NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,    v_dept_eng,   v_desig_srswe,   v_loc_dxb, v_ss_eng, v_emp_mgr,   '2023-01-01', NULL, 'Promotion', 'Promoted to Sr. SWE', NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 9. Emergency Contacts
-- ============================================================
INSERT INTO emergency_contacts (id, tenant_id, employee_id, name, relationship, phone_number, alternate_phone, email, address, is_primary, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_admin, 'Mariam Al-Hassan',    'Spouse',  '+971-50-2001001', '+971-50-2001011', 'mariam.hassan@gmail.com',    'Sheikh Zayed Road, Dubai',  true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr,   'Tariq Al-Demo',       'Father',  '+971-50-2002001', NULL,              NULL,                          'Al Barsha, Dubai',          true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_pa,    'Arun Sharma',         'Spouse',  '+91-98765-43210', '+91-98765-43211', 'arun.sharma@gmail.com',       'Mumbai, India',             true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e1,    'Fatma Hassan',        'Mother',  '+971-50-2004001', '+971-55-2004001', 'fatma.hassan@hotmail.com',    'Deira, Dubai',              true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,    'Khaled Ahmed',        'Brother', '+971-50-2005001', NULL,              'khaled.ahmed@gmail.com',      'Sharjah, UAE',              true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,    'Priya Kumar',         'Spouse',  '+91-99887-76543', '+91-99887-76544', 'priya.kumar@gmail.com',       'Bangalore, India',          true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e4,    'Nadia Ali',           'Mother',  '+971-50-2007001', '+971-55-2007001', NULL,                          'Al Ain, UAE',               true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,    'Emma Wilson',         'Spouse',  '+44-7911-123456', NULL,              'emma.wilson@outlook.com',     'London, UK',                true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e6,    'Hamid Khalid',        'Father',  '+971-50-2009001', '+971-55-2009001', NULL,                          'Abu Dhabi, UAE',            true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,    'Lin Wei',             'Mother',  '+86-138-0013-8000',NULL,             'lin.wei@163.com',             'Beijing, China',            true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e8,    'Amira Omar',          'Mother',  '+971-50-2011001', '+971-55-2011001', NULL,                          'Ajman, UAE',                true, NOW());

-- ============================================================
-- 10. Employee Bank Accounts
-- ============================================================
INSERT INTO employee_bank_accounts (id, tenant_id, employee_id, bank_name, account_holder_name, account_number, branch_code, swift_code, iban_number, bank_country, is_primary, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_admin, 'Emirates NBD',              'Demo Admin',    '1001234567890',  'ENBD001', 'EBILAEAD', 'AE070260001001234567890', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr,   'Abu Dhabi Commercial Bank', 'Demo Manager',  '1002345678901',  'ADCB001', 'ADCBAEAA', 'AE280030002345678901234', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_pa,    'First Abu Dhabi Bank',      'Priya Sharma',  '1003456789012',  'FAB0001', 'NBADAEAA', 'AE460350003456789012345', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e1,    'Emirates NBD',              'Ali Hassan',    '1004567890123',  'ENBD002', 'EBILAEAD', 'AE070260004567890123456', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,    'Mashreq Bank',              'Sara Ahmed',    '1005678901234',  'MASH001', 'BOMLAEAD', 'AE390330005678901234567', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,    'HDFC Bank',                 'Ravi Kumar',    '50100123456789', 'HDFC001', 'HDFCINBB', 'AE460350003456789098765', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e4,    'Abu Dhabi Commercial Bank', 'Fatima Ali',    '1007890123456',  'ADCB002', 'ADCBAEAA', 'AE280030007890123456789', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,    'Emirates NBD',              'James Wilson',  '1008901234567',  'ENBD003', 'EBILAEAD', 'AE070260008901234567890', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e6,    'First Abu Dhabi Bank',      'Nour Khalid',   '1009012345678',  'FAB0002', 'NBADAEAA', 'AE460350009012345678901', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,    'Emirates NBD',              'Chen Wei',      '1010123456789',  'ENBD004', 'EBILAEAD', 'AE070260010123456789012', 'AE', true, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e8,    'Mashreq Bank',              'Layla Omar',    '1011234567890',  'MASH002', 'BOMLAEAD', 'AE390330011234567890123', 'AE', true, NOW());

-- ============================================================
-- 11. Tenant Bank Account
-- ============================================================
INSERT INTO tenant_bank_accounts (id, tenant_id, bank_name, account_holder_name, account_number, branch_code, swift_code, iban_number, bank_country, is_primary, label, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, 'Emirates NBD',              'Demo Company LLC', '9001000000001', 'ENBD-CORP', 'EBILAEAD', 'AE070260009001000000001', 'AE', true,  'Primary Payroll Account', NOW()),
    (gen_random_uuid(), v_tenant_id, 'Abu Dhabi Commercial Bank', 'Demo Company LLC', '9002000000002', 'ADCB-CORP', 'ADCBAEAA', 'AE280030009002000000002', 'AE', false, 'Operations Account',      NOW());

-- ============================================================
-- 12. Public Holidays — UAE 2025 & 2026
-- ============================================================
INSERT INTO public_holidays (id, tenant_id, date, name, is_recurring, description, created_at)
VALUES
    -- 2025
    (gen_random_uuid(), v_tenant_id, '2025-01-01', 'New Year''s Day',          true,  'Gregorian New Year',                              NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-03-30', 'Eid Al Fitr',              false, 'End of Ramadan — Day 1 (subject to moon sighting)',NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-03-31', 'Eid Al Fitr Holiday',      false, 'End of Ramadan — Day 2',                          NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-04-01', 'Eid Al Fitr Holiday',      false, 'End of Ramadan — Day 3',                          NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-05-01', 'Labour Day',               true,  'International Workers'' Day',                     NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-06-06', 'Arafat Day (Eid Al Adha Eve)',false,'Day before Eid Al Adha',                        NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-06-07', 'Eid Al Adha',              false, 'Feast of Sacrifice — Day 1',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-06-08', 'Eid Al Adha Holiday',      false, 'Feast of Sacrifice — Day 2',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-06-09', 'Eid Al Adha Holiday',      false, 'Feast of Sacrifice — Day 3',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-06-27', 'Islamic New Year',         false, 'Hijri New Year 1447',                             NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-09-05', 'Prophet''s Birthday',      false, 'Mawlid Al Nabi',                                  NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-12-01', 'Commemoration Day',        true,  'Martyrs'' Day',                                   NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-12-02', 'UAE National Day',         true,  'Union Day — Day 1',                               NOW()),
    (gen_random_uuid(), v_tenant_id, '2025-12-03', 'UAE National Day Holiday', true,  'Union Day — Day 2',                               NOW()),
    -- 2026
    (gen_random_uuid(), v_tenant_id, '2026-01-01', 'New Year''s Day',          true,  'Gregorian New Year',                              NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-03-20', 'Eid Al Fitr',              false, 'End of Ramadan — Day 1 (subject to moon sighting)',NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-03-21', 'Eid Al Fitr Holiday',      false, 'End of Ramadan — Day 2',                          NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-03-22', 'Eid Al Fitr Holiday',      false, 'End of Ramadan — Day 3',                          NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-05-01', 'Labour Day',               true,  'International Workers'' Day',                     NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-05-27', 'Arafat Day (Eid Al Adha Eve)',false,'Day before Eid Al Adha',                        NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-05-28', 'Eid Al Adha',              false, 'Feast of Sacrifice — Day 1',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-05-29', 'Eid Al Adha Holiday',      false, 'Feast of Sacrifice — Day 2',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-05-30', 'Eid Al Adha Holiday',      false, 'Feast of Sacrifice — Day 3',                      NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-06-16', 'Islamic New Year',         false, 'Hijri New Year 1448',                             NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-07-17', 'Prophet''s Birthday',      false, 'Mawlid Al Nabi',                                  NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-12-01', 'Commemoration Day',        true,  'Martyrs'' Day',                                   NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-12-02', 'UAE National Day',         true,  'Union Day — Day 1',                               NOW()),
    (gen_random_uuid(), v_tenant_id, '2026-12-03', 'UAE National Day Holiday', true,  'Union Day — Day 2',                               NOW());

-- ============================================================
-- 13. Leave Balances (2025 + 2026)
-- ============================================================
INSERT INTO leave_balances (id, tenant_id, employee_id, leave_type_id, year, accrued, used, created_at)
SELECT gen_random_uuid(), v_tenant_id, emp, lt, yr, acc, used_days, NOW()
FROM (VALUES
    (v_emp_admin, v_lt_annual, 2025, 30.00, 5.00),  (v_emp_admin, v_lt_sick, 2025, 15.00, 2.00),  (v_emp_admin, v_lt_casual, 2025, 10.00, 1.00),
    (v_emp_mgr,   v_lt_annual, 2025, 30.00, 8.00),  (v_emp_mgr,   v_lt_sick, 2025, 15.00, 0.00),  (v_emp_mgr,   v_lt_casual, 2025, 10.00, 2.00),
    (v_emp_pa,    v_lt_annual, 2025, 30.00, 3.00),  (v_emp_pa,    v_lt_sick, 2025, 15.00, 1.00),  (v_emp_pa,    v_lt_casual, 2025, 10.00, 0.00),
    (v_emp_e1,    v_lt_annual, 2025, 30.00, 12.00), (v_emp_e1,    v_lt_sick, 2025, 15.00, 3.00),  (v_emp_e1,    v_lt_casual, 2025, 10.00, 2.00),
    (v_emp_e2,    v_lt_annual, 2025, 30.00, 6.00),  (v_emp_e2,    v_lt_sick, 2025, 15.00, 0.00),  (v_emp_e2,    v_lt_casual, 2025, 10.00, 1.00),
    (v_emp_e3,    v_lt_annual, 2025, 30.00, 10.00), (v_emp_e3,    v_lt_sick, 2025, 15.00, 5.00),  (v_emp_e3,    v_lt_casual, 2025, 10.00, 3.00),
    (v_emp_e4,    v_lt_annual, 2025, 30.00, 4.00),  (v_emp_e4,    v_lt_sick, 2025, 15.00, 0.00),  (v_emp_e4,    v_lt_casual, 2025, 10.00, 0.00),
    (v_emp_e5,    v_lt_annual, 2025, 30.00, 15.00), (v_emp_e5,    v_lt_sick, 2025, 15.00, 2.00),  (v_emp_e5,    v_lt_casual, 2025, 10.00, 4.00),
    (v_emp_e6,    v_lt_annual, 2025, 30.00, 2.00),  (v_emp_e6,    v_lt_sick, 2025, 15.00, 0.00),  (v_emp_e6,    v_lt_casual, 2025, 10.00, 1.00),
    (v_emp_e7,    v_lt_annual, 2025, 30.00, 7.00),  (v_emp_e7,    v_lt_sick, 2025, 15.00, 1.00),  (v_emp_e7,    v_lt_casual, 2025, 10.00, 2.00),
    (v_emp_e8,    v_lt_annual, 2025, 30.00, 0.00),  (v_emp_e8,    v_lt_sick, 2025, 15.00, 0.00),  (v_emp_e8,    v_lt_casual, 2025, 10.00, 0.00),
    -- 2026
    (v_emp_admin, v_lt_annual, 2026, 30.00, 0.00),  (v_emp_admin, v_lt_sick, 2026, 15.00, 0.00),  (v_emp_admin, v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_mgr,   v_lt_annual, 2026, 30.00, 2.00),  (v_emp_mgr,   v_lt_sick, 2026, 15.00, 0.00),  (v_emp_mgr,   v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_pa,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_pa,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_pa,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e1,    v_lt_annual, 2026, 30.00, 3.00),  (v_emp_e1,    v_lt_sick, 2026, 15.00, 1.00),  (v_emp_e1,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e2,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e2,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_e2,    v_lt_casual, 2026, 10.00, 1.00),
    (v_emp_e3,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e3,    v_lt_sick, 2026, 15.00, 2.00),  (v_emp_e3,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e4,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e4,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_e4,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e5,    v_lt_annual, 2026, 30.00, 5.00),  (v_emp_e5,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_e5,    v_lt_casual, 2026, 10.00, 1.00),
    (v_emp_e6,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e6,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_e6,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e7,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e7,    v_lt_sick, 2026, 15.00, 2.00),  (v_emp_e7,    v_lt_casual, 2026, 10.00, 0.00),
    (v_emp_e8,    v_lt_annual, 2026, 30.00, 0.00),  (v_emp_e8,    v_lt_sick, 2026, 15.00, 0.00),  (v_emp_e8,    v_lt_casual, 2026, 10.00, 0.00)
) AS t(emp, lt, yr, acc, used_days)
ON CONFLICT DO NOTHING;

-- ============================================================
-- 14. Leave Requests (varied — approved, pending, rejected)
-- ============================================================
INSERT INTO leave_requests (id, tenant_id, employee_id, leave_type_id, start_date, end_date, days_count, reason, status, approved_by, approved_at, approver_comments, created_at)
VALUES
    -- 2025 Approved
    (gen_random_uuid(), v_tenant_id, v_emp_e1, v_lt_annual, '2025-07-01', '2025-07-05', 5, 'Family vacation',              'Approved', v_mgr_user,   '2025-06-20 10:00:00+00', 'Approved. Enjoy!',                           '2025-06-15 09:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e3, v_lt_sick,   '2025-09-10', '2025-09-12', 3, 'Fever and flu',                'Approved', v_mgr_user,   '2025-09-10 08:30:00+00', 'Get well soon',                              '2025-09-10 08:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e5, v_lt_annual, '2025-12-22', '2025-12-31', 8, 'Year-end holiday',             'Approved', v_mgr_user,   '2025-12-01 11:00:00+00', 'Approved',                                   '2025-11-25 10:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr,v_lt_annual, '2025-08-10', '2025-08-17', 6, 'Annual leave',                 'Approved', v_admin_user, '2025-07-28 09:00:00+00', 'Approved',                                   '2025-07-25 09:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e2, v_lt_casual, '2025-10-05', '2025-10-05', 1, 'Personal errand',              'Approved', v_admin_user, '2025-10-04 17:00:00+00', 'OK',                                         '2025-10-04 14:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_pa, v_lt_annual, '2025-11-15', '2025-11-19', 3, 'Diwali holidays',              'Approved', v_admin_user, '2025-11-01 10:00:00+00', 'Approved',                                   '2025-10-28 10:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e7, v_lt_annual, '2025-10-01', '2025-10-05', 3, 'National Day trip',            'Approved', v_mgr_user,   '2025-09-20 10:00:00+00', 'Approved',                                   '2025-09-18 09:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e4, v_lt_annual, '2025-06-06', '2025-06-09', 4, 'Eid Al Adha',                  'Approved', v_admin_user, '2025-06-01 09:00:00+00', 'Approved',                                   '2025-05-28 09:00:00+00'),
    -- 2025 Rejected
    (gen_random_uuid(), v_tenant_id, v_emp_e6, v_lt_annual, '2025-11-01', '2025-11-10', 8, 'Extended leave',               'Rejected', v_admin_user, '2025-10-20 14:00:00+00', 'Peak period — cannot approve this duration', '2025-10-15 10:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e8, v_lt_annual, '2025-12-15', '2025-12-21', 5, 'Christmas trip',               'Rejected', v_admin_user, '2025-12-05 09:00:00+00', 'Too many requests this period',              '2025-12-01 10:00:00+00'),
    -- 2026 Pending
    (gen_random_uuid(), v_tenant_id, v_emp_e1, v_lt_annual, '2026-02-24', '2026-02-28', 3, 'Eid Al Fitr holiday trip',     'Pending',  NULL, NULL, NULL, '2026-02-15 10:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e7, v_lt_sick,   '2026-02-17', '2026-02-18', 2, 'Medical appointment',          'Pending',  NULL, NULL, NULL, '2026-02-17 07:30:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e4, v_lt_casual, '2026-02-20', '2026-02-20', 1, 'Bank work',                    'Pending',  NULL, NULL, NULL, '2026-02-18 09:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e3, v_lt_sick,   '2026-01-28', '2026-01-29', 2, 'Stomach infection',            'Pending',  NULL, NULL, NULL, '2026-01-28 07:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_emp_e6, v_lt_casual, '2026-02-12', '2026-02-12', 1, 'Family function',              'Pending',  NULL, NULL, NULL, '2026-02-10 12:00:00+00')
ON CONFLICT DO NOTHING;

-- ============================================================
-- 15. Payroll Runs (Oct–Dec 2025 Completed; Jan–Feb 2026 Draft)
-- ============================================================
INSERT INTO payroll_runs (id, tenant_id, month, year, status, processed_at, approved_by, approved_at, created_at)
VALUES
    (v_pr_oct, v_tenant_id, 10, 2025, 'Completed', '2025-10-31 18:00:00+00', v_admin_user, '2025-11-01 09:30:00+00', '2025-10-28 09:00:00+00'),
    (v_pr_nov, v_tenant_id, 11, 2025, 'Completed', '2025-11-30 18:00:00+00', v_admin_user, '2025-12-01 09:30:00+00', '2025-11-28 09:00:00+00'),
    (v_pr_dec, v_tenant_id, 12, 2025, 'Completed', '2025-12-31 18:00:00+00', v_admin_user, '2026-01-02 10:00:00+00', '2025-12-28 09:00:00+00'),
    (v_pr_jan, v_tenant_id,  1, 2026, 'Draft',     NULL,                     NULL,         NULL,                     '2026-01-28 09:00:00+00'),
    (v_pr_feb, v_tenant_id,  2, 2026, 'Draft',     NULL,                     NULL,         NULL,                     '2026-02-28 09:00:00+00')
ON CONFLICT DO NOTHING;

-- ============================================================
-- 16. Payslips (Oct, Nov, Dec 2025 — 11 employees each)
-- ============================================================
INSERT INTO payslips (id, tenant_id, payroll_run_id, employee_id, working_days, present_days, gross_earnings, total_deductions, net_pay, breakdown_json, created_at)
VALUES
    -- October 2025
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_admin, 23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_mgr,   23, 22, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_pa,    23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e1,    23, 20, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e2,    23, 22, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e3,    23, 23, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e4,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e5,    23, 23, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e6,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e7,    23, 20, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-10-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_oct, v_emp_e8,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-10-31 18:00:00+00'),
    -- November 2025
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_admin, 21, 21, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_mgr,   21, 20, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_pa,    21, 19, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e1,    21, 21, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e2,    21, 21, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e3,    21, 19, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e4,    21, 21, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e5,    21, 21, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e6,    21, 21, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e7,    21, 21, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-11-30 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_nov, v_emp_e8,    21, 21, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-11-30 18:00:00+00'),
    -- December 2025
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_admin, 23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_mgr,   23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_pa,    23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e1,    23, 18, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e2,    23, 23, 35000.00, 3300.00, 31700.00, '{"basic":25000,"hra":8000,"transport":2000,"tax":2500,"insurance":800}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e3,    23, 23, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e4,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e5,    23, 15, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e6,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e7,    23, 23, 21500.00, 1700.00, 19800.00, '{"basic":15000,"hra":5000,"transport":1500,"tax":1200,"insurance":500}', '2025-12-31 18:00:00+00'),
    (gen_random_uuid(), v_tenant_id, v_pr_dec, v_emp_e8,    23, 23, 14000.00, 1200.00, 12800.00, '{"basic":10000,"hra":3000,"transport":1000,"tax":800,"insurance":400}',  '2025-12-31 18:00:00+00')
ON CONFLICT DO NOTHING;

-- ============================================================
-- 17. Assets (7 — mix of assigned and available)
-- ============================================================
INSERT INTO assets (id, tenant_id, asset_code, asset_type, make, model, serial_number, purchase_date, purchase_price, status, created_at)
VALUES
    (v_asset1, v_tenant_id, 'ASSET-001', 'Laptop',     'Apple',    'MacBook Pro 16"',    'SN-MBP-001',  '2023-01-15', 12000.00, 'Assigned', NOW()),
    (v_asset2, v_tenant_id, 'ASSET-002', 'Laptop',     'Dell',     'XPS 15',             'SN-DXP-002',  '2022-06-10', 8500.00,  'Assigned', NOW()),
    (v_asset3, v_tenant_id, 'ASSET-003', 'Mobile',     'Apple',    'iPhone 15 Pro',      'SN-IP15-003', '2023-10-01', 4500.00,  'Assigned', NOW()),
    (v_asset4, v_tenant_id, 'ASSET-004', 'Monitor',    'LG',       '27UK850-W',          'SN-LGM-004',  '2022-03-20', 1800.00,  'Assigned', NOW()),
    (v_asset5, v_tenant_id, 'ASSET-005', 'Peripheral', 'Logitech', 'MX Keys',            'SN-LMK-005',  '2023-05-12', 350.00,   'Assigned', NOW()),
    (v_asset6, v_tenant_id, 'ASSET-006', 'Laptop',     'Lenovo',   'ThinkPad X1 Carbon', 'SN-TPX-006',  '2023-08-20', 9500.00,  'Assigned', NOW()),
    (v_asset7, v_tenant_id, 'ASSET-007', 'Mobile',     'Samsung',  'Galaxy S24 Ultra',   'SN-S24-007',  '2024-01-10', 3800.00,  'Available',NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 18. Asset Assignments
-- ============================================================
INSERT INTO asset_assignments (id, tenant_id, asset_id, employee_id, assigned_date, returned_date, assigned_condition, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_asset1, v_emp_e1,  '2023-01-20', NULL,         'New',  NOW()),
    (gen_random_uuid(), v_tenant_id, v_asset2, v_emp_e3,  '2022-06-15', NULL,         'New',  NOW()),
    (gen_random_uuid(), v_tenant_id, v_asset3, v_emp_mgr, '2023-10-05', NULL,         'New',  NOW()),
    (gen_random_uuid(), v_tenant_id, v_asset4, v_emp_e7,  '2022-04-01', NULL,         'Good', NOW()),
    (gen_random_uuid(), v_tenant_id, v_asset5, v_emp_e5,  '2023-05-15', NULL,         'New',  NOW()),
    (gen_random_uuid(), v_tenant_id, v_asset6, v_emp_e2,  '2023-08-25', NULL,         'New',  NOW()),
    -- Historical: returned
    (gen_random_uuid(), v_tenant_id, v_asset7, v_emp_pa,  '2024-01-15', '2024-12-31', 'Good', NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- 19. Attendance Logs — 5 months (Oct 2025 – Feb 2026, weekdays)
-- ============================================================
INSERT INTO attendance_logs (id, tenant_id, employee_id, date, clock_in, clock_out, status, is_late, late_by_minutes, is_regularized, created_at)
SELECT
    gen_random_uuid(), v_tenant_id, emp,
    log_date,
    (log_date::timestamp + INTERVAL '9 hours')::timestamptz,
    (log_date::timestamp + INTERVAL '18 hours')::timestamptz,
    'Present', false, 0, false, NOW()
FROM (VALUES
    (v_emp_admin),(v_emp_mgr),(v_emp_pa),(v_emp_e1),(v_emp_e2),
    (v_emp_e3),(v_emp_e4),(v_emp_e5),(v_emp_e6),(v_emp_e7),(v_emp_e8)
) AS emps(emp)
CROSS JOIN (
    SELECT d::date AS log_date
    FROM generate_series('2025-10-01'::date, '2026-02-28'::date, '1 day'::interval) d
    WHERE EXTRACT(DOW FROM d) NOT IN (0, 6)
    AND d NOT IN ('2025-12-01'::date, '2025-12-02'::date, '2025-12-03'::date, '2026-01-01'::date)
) AS dates
ON CONFLICT DO NOTHING;

-- Late arrivals: Ali Hassan (Nov)
UPDATE attendance_logs
SET is_late = true, late_by_minutes = 15,
    clock_in = (date::timestamp + INTERVAL '9 hours 15 minutes')::timestamptz
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e1
  AND date IN ('2025-11-03'::date, '2025-11-05'::date, '2025-11-12'::date);

-- Late arrivals: Ravi Kumar (Dec)
UPDATE attendance_logs
SET is_late = true, late_by_minutes = 20,
    clock_in = (date::timestamp + INTERVAL '9 hours 20 minutes')::timestamptz
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e3
  AND date IN ('2025-12-02'::date, '2025-12-03'::date);

-- Late arrival: Chen Wei (Jan — regularization pending)
UPDATE attendance_logs
SET is_late = true, late_by_minutes = 15,
    clock_in = (date::timestamp + INTERVAL '9 hours 15 minutes')::timestamptz
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e7 AND date = '2026-01-14'::date;

-- Absent: Ali Hassan Nov 20 (later regularized → Approved)
UPDATE attendance_logs
SET status = 'Absent', clock_in = NULL, clock_out = NULL, is_regularized = true
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e1 AND date = '2025-11-20'::date;

-- Absent: Fatima Ali Jan 27 (regularization pending)
UPDATE attendance_logs
SET status = 'Absent', clock_in = NULL, clock_out = NULL
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e4 AND date = '2026-01-27'::date;

-- Missing clock-in: James Wilson Feb 3 (regularization pending)
UPDATE attendance_logs
SET clock_in = NULL
WHERE tenant_id = v_tenant_id AND employee_id = v_emp_e5 AND date = '2026-02-03'::date;

-- ============================================================
-- 20. Attendance Regularization Requests
-- ============================================================
INSERT INTO attendance_regularization_requests (id, tenant_id, employee_id, attendance_date, requested_status, requested_clock_in, requested_clock_out, reason, status, reviewed_by, reviewed_at, reviewer_comments, created_at)
VALUES
    -- Ali Hassan: Nov 20 absent → Approved
    (gen_random_uuid(), v_tenant_id, v_emp_e1, '2025-11-20'::date, 'Present',
     '2025-11-20 09:00:00+00', '2025-11-20 18:00:00+00',
     'Was at hospital for emergency appointment. Doctor certificate submitted.',
     'Approved', v_mgr_user, '2025-11-21 09:30:00+00', 'Approved. Medical docs verified.',
     '2025-11-21 08:00:00+00'),
    -- Ravi Kumar: Dec 2 missed clock-out → Approved
    (gen_random_uuid(), v_tenant_id, v_emp_e3, '2025-12-02'::date, 'Present',
     '2025-12-02 09:00:00+00', '2025-12-02 19:00:00+00',
     'Forgot to clock out after staying late for sprint deadline.',
     'Approved', v_mgr_user, '2025-12-03 09:00:00+00', 'Approved. Badge logs confirm presence until 19:05.',
     '2025-12-02 20:00:00+00'),
    -- Chen Wei: Jan 14 late mark removal → Pending
    (gen_random_uuid(), v_tenant_id, v_emp_e7, '2026-01-14'::date, 'Present',
     '2026-01-14 09:00:00+00', '2026-01-14 18:00:00+00',
     'Metro delay due to technical fault. Requesting removal of late mark.',
     'Pending', NULL, NULL, NULL,
     '2026-01-14 10:00:00+00'),
    -- James Wilson: Feb 3 missing clock-in → Pending
    (gen_random_uuid(), v_tenant_id, v_emp_e5, '2026-02-03'::date, 'Present',
     '2026-02-03 09:00:00+00', '2026-02-03 18:00:00+00',
     'Forgot to clock in via mobile app. Was in office the full day — team can confirm.',
     'Pending', NULL, NULL, NULL,
     '2026-02-04 08:30:00+00'),
    -- Fatima Ali: Jan 27 absent → Pending
    (gen_random_uuid(), v_tenant_id, v_emp_e4, '2026-01-27'::date, 'Present',
     '2026-01-27 09:00:00+00', '2026-01-27 18:00:00+00',
     'Was on a client visit in Sharjah, forgot to mark attendance remotely.',
     'Pending', NULL, NULL, NULL,
     '2026-01-28 09:00:00+00');

-- ============================================================
-- 21. Employee Rosters (Feb–Mar 2026)
-- ============================================================
INSERT INTO employee_rosters (id, tenant_id, employee_id, shift_id, effective_date, created_at)
SELECT gen_random_uuid(), v_tenant_id, emp, v_shift_std, roster_date::timestamptz, NOW()
FROM (VALUES
    (v_emp_admin),(v_emp_mgr),(v_emp_pa),(v_emp_e1),(v_emp_e2),
    (v_emp_e3),(v_emp_e4),(v_emp_e5),(v_emp_e6),(v_emp_e7),(v_emp_e8)
) AS emps(emp)
CROSS JOIN (
    SELECT d::date AS roster_date
    FROM generate_series('2026-02-17'::date, '2026-03-31'::date, '1 day'::interval) d
    WHERE EXTRACT(DOW FROM d) NOT IN (0, 6)
) AS dates
ON CONFLICT DO NOTHING;

-- ============================================================
-- 22. Employee Qualifications
-- ============================================================
INSERT INTO employee_qualifications (id, tenant_id, employee_id, degree, field_of_study, institution, passing_year, grade, notes, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_mgr, 'Master of Business Administration', 'Business Management',  'University of Dubai',            2008, 'Distinction',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr, 'Bachelor of Engineering',           'Computer Engineering', 'American University of Sharjah', 2004, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e1,  'Bachelor of Science',               'Computer Science',     'IIT Bombay',                     2017, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,  'Bachelor of Science',               'Human Resource Mgmt',  'American University of Dubai',   2015, 'Second Class Upper',NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,  'Diploma in HR Management',          'HR Practices',         'CIPD London (Online)',           2019, 'Pass',              'CIPD Level 5', NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,  'Master of Technology',              'Software Engineering', 'BITS Pilani',                    2015, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,  'Bachelor of Technology',            'Computer Science',     'BITS Pilani',                    2013, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e4,  'Bachelor of Science',               'Business Admin',       'UAE University',                 2019, 'Second Class',      NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,  'Bachelor of Science',               'Computer Science',     'University College London',      2010, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e6,  'Bachelor of Science',               'Accounting & Finance', 'Zayed University',               2018, 'Second Class Upper',NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,  'Bachelor of Engineering',           'Software Engineering', 'Peking University',              2016, 'First Class',       NULL, NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e8,  'Bachelor of Commerce',              'Marketing',            'University of Ajman',            2021, 'Second Class',      NULL, NOW());

-- ============================================================
-- 23. Employee Work Experiences
-- ============================================================
INSERT INTO employee_work_experiences (id, tenant_id, employee_id, company_name, designation, from_date, to_date, is_current, responsibilities, reason_for_leaving, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_mgr, 'XYZ Technologies LLC',  'Senior Engineer',    '2008-06-01 00:00:00+00', '2014-12-31 00:00:00+00', false, 'Led a team of 8 engineers. Delivered 3 major product launches.',                                       'Career growth',              NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr, 'Alpha Solutions Dubai', 'Engineering Manager','2015-01-01 00:00:00+00', '2019-12-31 00:00:00+00', false, 'Managed engineering department. Drove agile transformation.',                                           'Joined Demo Company as founding member', NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e1,  'Accenture India',       'Software Intern',    '2016-06-01 00:00:00+00', '2016-12-31 00:00:00+00', false, 'React.js frontend and REST API integration for a banking client.',                                     'End of internship',          NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,  'Gulf HR Consultancy',   'HR Coordinator',     '2015-08-01 00:00:00+00', '2021-08-31 00:00:00+00', false, 'End-to-end recruitment, onboarding and performance management for 200+ employees.',                    'Moved to in-house HR role',  NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,  'Infosys BPO',           'Software Developer', '2015-07-01 00:00:00+00', '2020-10-31 00:00:00+00', false, 'Full-stack Java Spring Boot + Angular. 4 enterprise projects delivered.',                              'Relocated to UAE',           NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,  'ABC Tech Solutions',    'Senior Developer',   '2010-09-01 00:00:00+00', '2019-06-30 00:00:00+00', false, 'Led backend architecture for SaaS platform (50k users). Mentored 5 junior developers.',              'Moved to Dubai for senior role', NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,  'Baidu Inc.',            'Software Engineer',  '2016-07-01 00:00:00+00', '2021-04-30 00:00:00+00', false, 'Search ranking algorithms and mobile app features.',                                                   'International career in Dubai', NOW());

-- ============================================================
-- 24. Employee Certifications
-- ============================================================
INSERT INTO employee_certifications (id, tenant_id, employee_id, certification_name, issuing_organization, certificate_number, issue_date, expiry_date, has_expiry, notes, created_at)
VALUES
    (gen_random_uuid(), v_tenant_id, v_emp_e1,  'AWS Certified Developer – Associate',           'Amazon Web Services', 'AWS-DEV-2024-001',  '2024-03-15 00:00:00+00', '2027-03-15 00:00:00+00', true,  'Scored 890/1000',            NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,  'Microsoft Azure Solutions Architect Expert',    'Microsoft',           'AZ-305-2023-001',   '2023-08-10 00:00:00+00', '2026-08-10 00:00:00+00', true,  NULL,                         NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e3,  'Certified Scrum Master (CSM)',                  'Scrum Alliance',      'CSM-2022-112345',   '2022-02-20 00:00:00+00', '2024-02-20 00:00:00+00', true,  'Needs renewal',              NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,  'Project Management Professional (PMP)',         'PMI',                 'PMP-2021-987654',   '2021-05-05 00:00:00+00', '2027-05-05 00:00:00+00', true,  NULL,                         NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e5,  'Professional Scrum Master I (PSM I)',           'Scrum.org',           'PSM1-2020-54321',   '2020-11-10 00:00:00+00', NULL,                     false, 'Lifetime certificate',       NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e7,  'Google Cloud Professional Cloud Architect',    'Google Cloud',        'GCP-2024-080808',   '2024-01-22 00:00:00+00', '2027-01-22 00:00:00+00', true,  NULL,                         NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_mgr, 'Certified Information Systems Manager (CISM)', 'ISACA',               'CISM-2019-200111',  '2019-09-15 00:00:00+00', '2025-09-15 00:00:00+00', true,  'Up for renewal',             NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e2,  'CIPD Level 5 Associate Diploma in People Mgmt','CIPD',                'CIPD5-2019-10099',  '2019-12-01 00:00:00+00', NULL,                     false, NULL,                         NOW()),
    (gen_random_uuid(), v_tenant_id, v_emp_e6,  'Certified Public Accountant (CPA)',             'AICPA',               'CPA-2022-456789',   '2022-07-01 00:00:00+00', NULL,                     false, NULL,                         NOW());

-- ============================================================
-- 25. Action Center Tasks (all roles, varied statuses)
-- ============================================================
INSERT INTO user_tasks (id, tenant_id, owner_user_id, title, entity_type, entity_id, status, action_url, priority, due_date, created_at)
VALUES
    -- Leave approvals (Admin)
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Review pending leave — Fatima Ali (Casual, Feb 20)',
     'LEAVE', v_emp_e4, 'Pending', '/leave/requests', 'High', NOW() + INTERVAL '1 day', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Review pending leave — Ali Hassan (Annual, Feb 24–28)',
     'LEAVE', v_emp_e1, 'Pending', '/leave/requests', 'High', NOW() + INTERVAL '1 day', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Review pending leave — Ravi Kumar (Sick, Jan 28–29)',
     'LEAVE', v_emp_e3, 'Pending', '/leave/requests', 'Normal', NOW() + INTERVAL '2 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Review pending leave — Nour Khalid (Casual, Feb 12)',
     'LEAVE', v_emp_e6, 'Pending', '/leave/requests', 'Low', NOW() + INTERVAL '3 days', NOW()),
    -- Leave (Manager)
    (gen_random_uuid(), v_tenant_id, v_mgr_user,
     'Follow up: Chen Wei sick leave pending since Feb 17',
     'LEAVE', v_emp_e7, 'Pending', '/leave/requests', 'Normal', NOW() + INTERVAL '2 days', NOW()),
    -- Attendance regularization (Manager)
    (gen_random_uuid(), v_tenant_id, v_mgr_user,
     'Review regularization — Chen Wei (Jan 14, late mark removal)',
     'ATTENDANCE', v_emp_e7, 'Pending', '/attendance', 'Normal', NOW() + INTERVAL '3 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_mgr_user,
     'Review regularization — James Wilson (Feb 3, missing clock-in)',
     'ATTENDANCE', v_emp_e5, 'Pending', '/attendance', 'High', NOW(), NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Review regularization — Fatima Ali (Jan 27, client visit)',
     'ATTENDANCE', v_emp_e4, 'Pending', '/attendance', 'Normal', NOW() + INTERVAL '2 days', NOW()),
    -- Assets (Admin)
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Annual asset audit due — MacBook Pro 16" (ASSET-001)',
     'ASSET', v_asset1, 'Pending', '/assets', 'Normal', NOW() + INTERVAL '7 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Warranty expiry soon — iPhone 15 Pro (ASSET-003)',
     'ASSET', v_asset3, 'Pending', '/assets', 'Low', NOW() + INTERVAL '30 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Assign available asset — Samsung Galaxy S24 (ASSET-007)',
     'ASSET', v_asset7, 'Pending', '/assets', 'Low', NOW() + INTERVAL '14 days', NOW()),
    -- Payroll (Payroll Admin)
    (gen_random_uuid(), v_tenant_id, v_pa_user,
     'Run January 2026 payroll — currently in Draft',
     'PAYROLL', v_pr_jan, 'Pending', '/payroll/runs', 'High', NOW(), NOW()),
    (gen_random_uuid(), v_tenant_id, v_pa_user,
     'Process February 2026 payroll by month-end',
     'PAYROLL', v_pr_feb, 'Pending', '/payroll/runs', 'Normal', NOW() + INTERVAL '10 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_pa_user,
     'Verify salary structure for Layla Omar (EMP011)',
     'PAYROLL', v_emp_e8, 'Pending', '/payroll/structures', 'Normal', NOW() + INTERVAL '5 days', NOW()),
    -- HR (Admin)
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Probation review due — Layla Omar (joined Aug 2023)',
     'HR', v_emp_e8, 'Pending', '/employees', 'High', NOW(), NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Visa expiry in 60 days — Nour Khalid (expires Mar 31, 2026)',
     'HR', v_emp_e6, 'Pending', '/employees', 'High', NOW() + INTERVAL '5 days', NOW()),
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Visa expiry in 90 days — Sara Ahmed (expires Aug 31, 2026)',
     'HR', v_emp_e2, 'Pending', '/employees', 'Normal', NOW() + INTERVAL '14 days', NOW()),
    -- Completed tasks (show history)
    (gen_random_uuid(), v_tenant_id, v_admin_user,
     'Onboarding checklist completed — Chen Wei (EMP010)',
     'HR', v_emp_e7, 'Completed', '/employees', 'Normal', NOW() - INTERVAL '30 days', NOW() - INTERVAL '45 days'),
    (gen_random_uuid(), v_tenant_id, v_pa_user,
     'December 2025 payroll approved and disbursed',
     'PAYROLL', v_pr_dec, 'Completed', '/payroll/runs', 'High', NOW() - INTERVAL '55 days', NOW() - INTERVAL '60 days')
ON CONFLICT DO NOTHING;

RAISE NOTICE '✅ MyHRMS demo seed data loaded successfully!';
RAISE NOTICE '   Tenant         : Demo Company (ID: 10000000-0000-4000-8000-000000000001)';
RAISE NOTICE '   Employees      : 11 (EMP001–EMP011)';
RAISE NOTICE '   Salary         : 8 components, 3 structures (Mgmt/Engineering/Standard)';
RAISE NOTICE '   Payroll        : 5 runs (Oct–Dec 2025 Completed, Jan–Feb 2026 Draft)';
RAISE NOTICE '   Payslips       : 33 (3 months × 11 employees)';
RAISE NOTICE '   Leave          : 15 requests (8 Approved, 5 Pending, 2 Rejected)';
RAISE NOTICE '   Assets         : 7 (6 assigned, 1 available)';
RAISE NOTICE '   Attendance     : Oct 2025–Feb 2026 (5 months × 11 employees)';
RAISE NOTICE '   Regularization : 5 requests (2 Approved, 3 Pending)';
RAISE NOTICE '   Public Holidays: 28 UAE holidays (2025 + 2026)';
RAISE NOTICE '   Emergency Ctcts: 11 records (1 per employee)';
RAISE NOTICE '   Bank Accounts  : 11 employee + 2 tenant accounts';
RAISE NOTICE '   Qualifications : 12 records';
RAISE NOTICE '   Work Experience: 7 records';
RAISE NOTICE '   Certifications : 9 records';
RAISE NOTICE '   Action Tasks   : 19 (17 Pending, 2 Completed)';

END $$;
