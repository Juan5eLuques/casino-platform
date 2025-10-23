# Script PowerShell para crear SUPER_ADMIN en la base de datos
# Ejecuta el archivo create-superadmin.sql

param(
    [string]$DbHost = "localhost",
    [string]$DbPort = "5432",
    [string]$DbUser = "postgres",
    [string]$DbName = "casino_platform",
    [string]$DbPassword = ""
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Creating SUPER_ADMIN user" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Database configuration:" -ForegroundColor Yellow
Write-Host "  Host: $DbHost" -ForegroundColor White
Write-Host "  Port: $DbPort" -ForegroundColor White
Write-Host "  Database: $DbName" -ForegroundColor White
Write-Host "  User: $DbUser" -ForegroundColor White
Write-Host ""

# Verificar si psql está disponible
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue

if ($null -eq $psqlPath) {
    Write-Host "? Error: psql not found in PATH" -ForegroundColor Red
    Write-Host ""
Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Install PostgreSQL client tools, or" -ForegroundColor White
    Write-Host "  2. Use pgAdmin/DBeaver and run scripts/create-superadmin.sql manually" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Configurar password si se proporcionó
if ($DbPassword) {
    $env:PGPASSWORD = $DbPassword
}

# Ejecutar script SQL
try {
    $scriptPath = "scripts\create-superadmin.sql"
    
    if (!(Test-Path $scriptPath)) {
        Write-Host "? Error: Script not found: $scriptPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "Executing SQL script..." -ForegroundColor Yellow
    Write-Host ""

    psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -f $scriptPath

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
      Write-Host "==========================================" -ForegroundColor Green
        Write-Host "? SUPER_ADMIN created successfully!" -ForegroundColor Green
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Credentials:" -ForegroundColor Cyan
    Write-Host "  Username: superadmin" -ForegroundColor White
        Write-Host "  Password: password" -ForegroundColor White
Write-Host ""
   Write-Host "??  IMPORTANT: Change this password in production!" -ForegroundColor Yellow
        Write-Host ""
    } else {
        throw "psql command failed with exit code $LASTEXITCODE"
  }
}
catch {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Red
    Write-Host "? Error creating SUPER_ADMIN" -ForegroundColor Red
    Write-Host "==========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check:" -ForegroundColor Yellow
    Write-Host "  - PostgreSQL is running" -ForegroundColor White
    Write-Host "  - Database connection settings are correct" -ForegroundColor White
    Write-Host "  - Database '$DbName' exists" -ForegroundColor White
    Write-Host ""
    Write-Host "Alternative:" -ForegroundColor Yellow
    Write-Host "  Copy the content of scripts\create-superadmin.sql" -ForegroundColor White
    Write-Host "  and run it manually in pgAdmin or DBeaver" -ForegroundColor White
    Write-Host ""
    exit 1
}
finally {
    # Limpiar password del ambiente
    if ($DbPassword) {
    Remove-Item env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}
