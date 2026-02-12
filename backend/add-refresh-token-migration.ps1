#!/usr/bin/env pwsh
# Script to add RefreshToken migration

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Adding RefreshToken Migration" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Set location to backend directory
Set-Location -Path $PSScriptRoot

Write-Host "[1/3] Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

Write-Host "[2/3] Creating RefreshToken migration..." -ForegroundColor Yellow
dotnet ef migrations add AddRefreshToken `
    --project src/AlfTekPro.Infrastructure/AlfTekPro.Infrastructure.csproj `
    --startup-project src/AlfTekPro.API/AlfTekPro.API.csproj `
    --context HrmsDbContext `
    --output-dir Data/Migrations

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration creation failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Migration created successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "[3/3] Applying migration to database..." -ForegroundColor Yellow
dotnet ef database update `
    --project src/AlfTekPro.Infrastructure/AlfTekPro.Infrastructure.csproj `
    --startup-project src/AlfTekPro.API/AlfTekPro.API.csproj `
    --context HrmsDbContext

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Migration applied successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  ✅ RefreshToken Table Created!" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Verify the table was created:" -ForegroundColor Yellow
Write-Host 'docker exec -it alftekpro-postgres psql -U hrms_user -d alftekpro_hrms -c "\d refresh_tokens"' -ForegroundColor Cyan
