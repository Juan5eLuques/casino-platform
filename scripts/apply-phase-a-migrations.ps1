# Script: apply-phase-a-migrations.ps1
# Purpose: Apply all Phase A migrations in correct order (Windows)
# Date: 2025-01-22

$ErrorActionPreference = "Stop"

$DB_NAME = if ($env:DB_NAME) { $env:DB_NAME } else { "casino_db" }
$DB_USER = if ($env:DB_USER) { $env:DB_USER } else { "postgres" }
$DB_HOST = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$DB_PORT = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }

$MIGRATION_DIR = ".\apps\Casino.Infrastructure\Migrations"

Write-Host "?? Starting Phase A Migrations" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Database: $DB_NAME"
Write-Host "Host: ${DB_HOST}:${DB_PORT}"
Write-Host "User: $DB_USER"
Write-Host ""

function Execute-Migration {
    param (
        [string]$MigrationFile
    )
    
    $MigrationName = Split-Path $MigrationFile -Leaf
    
    Write-Host "?? Applying: $MigrationName" -ForegroundColor Yellow
    
    try {
        $env:PGPASSWORD = ""  # Set password if needed
        & psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME `
               -f $MigrationFile `
               -v ON_ERROR_STOP=1 `
               --echo-errors
        
        Write-Host "? $MigrationName completed" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "? $MigrationName FAILED" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host "Stopping migration process"
        exit 1
    }
}

# Execute migrations in order
Execute-Migration "$MIGRATION_DIR\001_AddAdminHierarchy.sql"
Execute-Migration "$MIGRATION_DIR\002_AddTransactionMetadata.sql"
Execute-Migration "$MIGRATION_DIR\003_CreateCommissionAccruals.sql"
Execute-Migration "$MIGRATION_DIR\004_CreateMonthlyClosures.sql"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "? Phase A Migrations Completed" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:"
Write-Host "  ? Added multilevel hierarchy to BackofficeUsers"
Write-Host "  ? Added metadata fields to WalletTransactions"
Write-Host "  ? Created commission_accruals table"
Write-Host "  ? Created monthly_closures table"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Verify data: psql -d $DB_NAME -c 'SELECT * FROM `"BackofficeUsers`" LIMIT 5;'"
Write-Host "  2. Check tables: psql -d $DB_NAME -c '\dt commission_accruals'"
Write-Host "  3. Proceed with Phase B (implement services)"
