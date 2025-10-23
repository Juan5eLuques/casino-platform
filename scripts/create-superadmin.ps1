# Script PowerShell para crear usuario SUPER_ADMIN
# Genera el hash de la contraseña usando el mismo PasswordHasher que la aplicación
# 
# USO:
#   .\create-superadmin.ps1
#   .\create-superadmin.ps1 -Password "tuContraseña"
#   .\create-superadmin.ps1 -Username "admin" -Password "miPassword"

param(
    [string]$Username = "superadmin",
  [string]$Password = "password",
    [string]$ConnectionString = "Host=localhost;Database=casino_platform;Username=postgres;Password=postgres"
)

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Create SUPER_ADMIN Script" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si existe Microsoft.AspNetCore.Identity
$identityAssembly = "Microsoft.AspNetCore.Identity"
try {
    Add-Type -AssemblyName $identityAssembly -ErrorAction Stop
    Write-Host "? Microsoft.AspNetCore.Identity cargado" -ForegroundColor Green
} catch {
    Write-Host "? Error: No se pudo cargar Microsoft.AspNetCore.Identity" -ForegroundColor Red
    Write-Host "  Instalando paquete NuGet..." -ForegroundColor Yellow
    
    # Intentar instalar el paquete
    try {
        Install-Package Microsoft.AspNetCore.Identity -Force -Scope CurrentUser
  Add-Type -AssemblyName $identityAssembly
    } catch {
        Write-Host "? No se pudo instalar automáticamente." -ForegroundColor Red
  Write-Host ""
        Write-Host "SOLUCIÓN MANUAL:" -ForegroundColor Yellow
        Write-Host "1. Ejecuta este script desde la carpeta del proyecto Casino.Api:" -ForegroundColor White
        Write-Host "   cd apps\api\Casino.Api" -ForegroundColor Gray
 Write-Host "   dotnet run --generate-superadmin-hash" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. O usa directamente el hash de ejemplo en el archivo:" -ForegroundColor White
   Write-Host "   scripts\create-superadmin.sql" -ForegroundColor Gray
     exit 1
    }
}

Write-Host ""
Write-Host "Generando hash para la contraseña..." -ForegroundColor Yellow

# Crear instancia del PasswordHasher
$hasherType = [Microsoft.AspNetCore.Identity.PasswordHasher[object]]
$hasher = New-Object $hasherType

# Generar hash
$passwordHash = $hasher.HashPassword($null, $Password)

Write-Host "? Hash generado exitosamente" -ForegroundColor Green
Write-Host ""

# Crear SQL dinámico
$sql = @"
-- Generado automáticamente por create-superadmin.ps1
-- Usuario: $Username
-- Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

BEGIN;

-- Eliminar usuario existente si existe
DELETE FROM "BackofficeUsers" WHERE "Username" = '$Username';

-- Insertar SUPER_ADMIN
INSERT INTO "BackofficeUsers" (
    "Id",
    "BrandId",
    "Username",
    "PasswordHash",
    "Role",
    "Status",
    "CreatedAt",
    "LastLoginAt",
"ParentCashierId",
    "ParentAdminId",
    "HierarchyLevel",
    "HierarchyPath",
    "CommissionPercent",
    "CreatedByUserId",
    "CreatedByRole",
    "WalletBalance"
)
VALUES (
    gen_random_uuid(),
    NULL,
    '$Username',
    '$passwordHash',
    'SUPER_ADMIN',
    'ACTIVE',
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    NULL,
    0,
    NULL,
    0,
    NULL,
    NULL,
    0.00
);

-- Verificar inserción
SELECT 
  "Id",
  "Username",
    "Role",
    "Status",
 "HierarchyLevel",
    "CreatedAt"
FROM "BackofficeUsers"
WHERE "Username" = '$Username';

COMMIT;
"@

# Guardar SQL en archivo
$sqlFilePath = "scripts/create-superadmin-generated.sql"
$sql | Out-File -FilePath $sqlFilePath -Encoding UTF8

Write-Host "? Script SQL generado: $sqlFilePath" -ForegroundColor Green
Write-Host ""

# Preguntar si desea ejecutar el script
Write-Host "¿Deseas ejecutar el script en la base de datos ahora? (S/N)" -ForegroundColor Cyan
$response = Read-Host

if ($response -eq "S" -or $response -eq "s" -or $response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "Ejecutando script en la base de datos..." -ForegroundColor Yellow
    
    try {
        # Verificar si psql está disponible
        $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
   
        if ($null -eq $psqlPath) {
        Write-Host "? Error: psql no encontrado en el PATH" -ForegroundColor Red
Write-Host "  Ejecuta manualmente el archivo: $sqlFilePath" -ForegroundColor Yellow
            exit 1
   }
  
 # Ejecutar con psql
  $env:PGPASSWORD = ($ConnectionString -split "Password=" -split ";")[1]
        $host = ($ConnectionString -split "Host=" -split ";")[1]
  $database = ($ConnectionString -split "Database=" -split ";")[1]
      $user = ($ConnectionString -split "Username=" -split ";")[1]
  
        psql -h $host -U $user -d $database -f $sqlFilePath
        
        Write-Host ""
        Write-Host "? Usuario SUPER_ADMIN creado exitosamente" -ForegroundColor Green
     Write-Host ""
    Write-Host "Credenciales:" -ForegroundColor Cyan
        Write-Host "  Username: $Username" -ForegroundColor White
        Write-Host "  Password: $Password" -ForegroundColor White
     
    } catch {
        Write-Host "? Error ejecutando el script: $_" -ForegroundColor Red
        Write-Host "  Ejecuta manualmente el archivo: $sqlFilePath" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "Script SQL guardado en: $sqlFilePath" -ForegroundColor Yellow
    Write-Host "Ejecuta manualmente con:" -ForegroundColor White
    Write-Host "  psql -h localhost -U postgres -d casino_platform -f $sqlFilePath" -ForegroundColor Gray
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
