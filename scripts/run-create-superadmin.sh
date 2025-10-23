#!/bin/bash

# Script para crear SUPER_ADMIN en la base de datos
# Ejecuta el archivo create-superadmin.sql

echo "=========================================="
echo "Creating SUPER_ADMIN user"
echo "=========================================="
echo ""

# Configuración por defecto
DB_HOST="${DB_HOST:-localhost}"
DB_USER="${DB_USER:-postgres}"
DB_NAME="${DB_NAME:-casino_platform}"
DB_PORT="${DB_PORT:-5432}"

echo "Database configuration:"
echo "  Host: $DB_HOST"
echo "  Port: $DB_PORT"
echo "  Database: $DB_NAME"
echo "  User: $DB_USER"
echo ""

# Ejecutar script SQL
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f scripts/create-superadmin.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "? SUPER_ADMIN created successfully!"
    echo "=========================================="
    echo ""
    echo "Credentials:"
    echo "  Username: superadmin"
    echo "  Password: password"
    echo ""
    echo "??  IMPORTANT: Change this password in production!"
    echo ""
else
    echo ""
    echo "=========================================="
    echo "? Error creating SUPER_ADMIN"
    echo "=========================================="
    echo ""
    echo "Please check:"
    echo "  - PostgreSQL is running"
    echo "  - Database connection settings are correct"
    echo "  - You have psql installed"
    echo ""
    exit 1
fi
