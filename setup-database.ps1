#!/usr/bin/env pwsh
# AlfTekPro HRMS - Database Setup Script
# Orchestrates PostgreSQL startup, migration, and seeding

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  AlfTekPro HRMS - Database Setup Orchestrator" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

# 1. Start PostgreSQL
Write-Host "[1/4] Ensuring PostgreSQL is running..." -ForegroundColor Yellow
docker-compose up -d postgres
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start PostgreSQL via Docker Compose." -ForegroundColor Red
    exit 1
}

# 2. Wait for PostgreSQL health
Write-Host "[2/4] Waiting for database to be ready..." -ForegroundColor Yellow
$retries = 30
$ready = $false
while ($retries -gt 0 -and -not $ready) {
    $check = docker exec alftekpro-postgres pg_isready -U hrms_user -d alftekpro_hrms 2>&1
    if ($check -match "accepting connections") {
        $ready = $true
    } else {
        $retries--
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
}
Write-Host ""
if (-not $ready) {
    Write-Host "❌ PostgreSQL did not become ready in time." -ForegroundColor Red
    exit 1
}

# 3. Run Migrations
Write-Host "[3/4] Running migrations..." -ForegroundColor Yellow
# Try to run via the newly created utility
if (Test-Path ".\run-migration.ps1") {
    .\run-migration.ps1
} else {
    # Fallback if utility missing
    docker exec -it alftekpro-api dotnet ef database update
}

# 4. Seed Data
Write-Host "[4/4] Seeding initial data (Regions, Demo Tenant)..." -ForegroundColor Yellow
docker exec -it alftekpro-api dotnet run --project /app/src/AlfTekPro.API -- seed
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Seeding might have failed or data already exists." -ForegroundColor Yellow
}

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "🎉 Database setup complete!" -ForegroundColor Green
Write-Host "You can now access Swagger at http://localhost:5001/swagger" -ForegroundColor Gray
Write-Host "===========================================" -ForegroundColor Cyan
