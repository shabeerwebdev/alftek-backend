#!/usr/bin/env pwsh
# AlfTekPro HRMS - Database Migration Script
# Supports both Docker (default) and Local execution

param(
    [Parameter(Mandatory=$false)]
    [switch]$Local,
    
    [Parameter(Mandatory=$false)]
    [string]$Name
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  AlfTekPro HRMS - Migration Utility" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

if ($Name) {
    Write-Host "Creating new migration: $Name..." -ForegroundColor Yellow
    if ($Local) {
        dotnet ef migrations add $Name --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API --output-dir Data/Migrations
    } else {
        docker exec -it alftekpro-api dotnet ef migrations add $Name --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API --output-dir Data/Migrations
    }
}

Write-Host "Applying migrations..." -ForegroundColor Yellow
if ($Local) {
    dotnet ef database update --project src/AlfTekPro.Infrastructure --startup-project src/AlfTekPro.API
} else {
    docker exec -it alftekpro-api dotnet ef database update
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Migration failed!" -ForegroundColor Red
}
