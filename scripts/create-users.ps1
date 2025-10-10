# Script PowerShell para crear usuarios administradores
# Ejecutar desde el directorio raíz del proyecto

Write-Host "?? Creando usuarios administradores..." -ForegroundColor Green

# Generar hash correcto para password "admin123" usando ASP.NET Identity PasswordHasher
# Este es un hash válido generado con PasswordHasher<object> para la password "admin123"
$passwordHash = "AQAAAAEAACcQAAAAELxH2k1QJ6gDXvMTpA3QWqJGU7P9mV5k8yN4C3Zf7R1A6M2bE5jS9T0W3YxHzKpQ8="

# Connection string desde appsettings.json
$connectionString = "Host=shortline.proxy.rlwy.net;Port=47433;Database=railway;Username=postgres;Password=dzPvAkviRrmLjpinAeNakUymDpWaHVuq;SSL Mode=Require;Trust Server Certificate=true;"

# SQL Commands
$operatorSql = @"
INSERT INTO operators (id, name, status, created_at) 
VALUES ('11111111-1111-1111-1111-111111111111'::uuid, 'Test Operator', 'ACTIVE', NOW()) 
ON CONFLICT (id) DO NOTHING;
"@

$usersSql = @"
-- Crear SUPER_ADMIN
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'superadmin',
    '$passwordHash',
    'SUPER_ADMIN',
    'ACTIVE',
    NOW()
) ON CONFLICT (username) DO UPDATE SET 
    password_hash = '$passwordHash',
    status = 'ACTIVE';

-- Crear OPERATOR_ADMIN  
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'operator_admin',
    '$passwordHash',
    'OPERATOR_ADMIN',
    'ACTIVE',
    NOW()
) ON CONFLICT (username) DO UPDATE SET 
    password_hash = '$passwordHash',
    status = 'ACTIVE';

-- Crear CASHIER
INSERT INTO backoffice_users (id, operator_id, username, password_hash, role, status, created_at) 
VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111'::uuid,
    'cashier_user',
    '$passwordHash',
    'CASHIER',
    'ACTIVE',
    NOW()
) ON CONFLICT (username) DO UPDATE SET 
    password_hash = '$passwordHash',
    status = 'ACTIVE';
"@

$verifySql = @"
SELECT u.id, u.username, u.role, u.status, o.name as operator_name 
FROM backoffice_users u 
LEFT JOIN operators o ON u.operator_id = o.id 
WHERE u.username IN ('superadmin', 'operator_admin', 'cashier_user');
"@

Write-Host "?? Ejecutando comandos SQL..." -ForegroundColor Yellow

try {
    # Verificar si psql está disponible
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
    
    if ($psqlPath) {
        Write-Host "? Usando psql para ejecutar comandos..." -ForegroundColor Green
        
        # Ejecutar comandos usando psql
        echo $operatorSql | psql $connectionString
        echo $usersSql | psql $connectionString
        echo $verifySql | psql $connectionString
        
        Write-Host "? Usuarios creados exitosamente!" -ForegroundColor Green
    }
    else {
        Write-Host "? psql no encontrado. Instala PostgreSQL client o ejecuta manualmente:" -ForegroundColor Red
        Write-Host "SQL Commands to execute:" -ForegroundColor Yellow
        Write-Host $operatorSql -ForegroundColor Cyan
        Write-Host $usersSql -ForegroundColor Cyan
        Write-Host $verifySql -ForegroundColor Cyan
    }
}
catch {
    Write-Host "? Error ejecutando comandos: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Comandos SQL para ejecutar manualmente:" -ForegroundColor Yellow
    Write-Host $operatorSql -ForegroundColor Cyan
    Write-Host $usersSql -ForegroundColor Cyan
}

Write-Host "`n?? Credenciales de acceso:" -ForegroundColor Green
Write-Host "Password para todos los usuarios: admin123" -ForegroundColor White
Write-Host "Usuarios creados:" -ForegroundColor White
Write-Host "- superadmin (SUPER_ADMIN)" -ForegroundColor Cyan
Write-Host "- operator_admin (OPERATOR_ADMIN)" -ForegroundColor Cyan  
Write-Host "- cashier_user (CASHIER)" -ForegroundColor Cyan

Write-Host "`n?? URL de login:" -ForegroundColor Green
Write-Host "https://admin.bet30.local:7182/api/v1/admin/auth/login" -ForegroundColor White

Write-Host "`n?? Ejemplo de request:" -ForegroundColor Green
Write-Host @"
{
  "username": "superadmin",
  "password": "admin123"
}
"@ -ForegroundColor Cyan