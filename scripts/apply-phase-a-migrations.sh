#!/bin/bash
# Script: apply-phase-a-migrations.sh
# Purpose: Apply all Phase A migrations in correct order
# Date: 2025-01-22

set -e  # Exit on error

DB_NAME="${DB_NAME:-casino_db}"
DB_USER="${DB_USER:-postgres}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"

MIGRATION_DIR="./apps/Casino.Infrastructure/Migrations"

echo "?? Starting Phase A Migrations"
echo "================================"
echo "Database: $DB_NAME"
echo "Host: $DB_HOST:$DB_PORT"
echo "User: $DB_USER"
echo ""

# Function to execute migration
execute_migration() {
    local migration_file=$1
    local migration_name=$(basename "$migration_file")
    
    echo "?? Applying: $migration_name"
    
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
         -f "$migration_file" \
         -v ON_ERROR_STOP=1 \
         --echo-errors
    
    if [ $? -eq 0 ]; then
        echo "? $migration_name completed"
        echo ""
    else
        echo "? $migration_name FAILED"
        echo "Stopping migration process"
        exit 1
    fi
}

# Execute migrations in order
execute_migration "$MIGRATION_DIR/001_AddAdminHierarchy.sql"
execute_migration "$MIGRATION_DIR/002_AddTransactionMetadata.sql"
execute_migration "$MIGRATION_DIR/003_CreateCommissionAccruals.sql"
execute_migration "$MIGRATION_DIR/004_CreateMonthlyClosures.sql"

echo "================================"
echo "? Phase A Migrations Completed"
echo ""
echo "Summary:"
echo "  ? Added multilevel hierarchy to BackofficeUsers"
echo "  ? Added metadata fields to WalletTransactions"
echo "  ? Created commission_accruals table"
echo "  ? Created monthly_closures table"
echo ""
echo "Next steps:"
echo "  1. Verify data with: psql -d $DB_NAME -c 'SELECT * FROM \"BackofficeUsers\" LIMIT 5;'"
echo "  2. Check new tables: psql -d $DB_NAME -c '\dt commission_accruals'"
echo "  3. Proceed with Phase B (implement services)"
