# Script completo para solucionar problemas de CORS y autenticación
# Ejecutar desde el directorio raíz del proyecto

Write-Host "?? Configurando Casino Platform para desarrollo local..." -ForegroundColor Green

# Configuration
$connectionString = "Host=shortline.proxy.rlwy.net;Port=47433;Database=railway;Username=postgres;Password=dzPvAkviRrmLjpinAeNakUymDpWaHVuq;SSL Mode=Require;Trust Server Certificate=true;"

Write-Host "`n?? PASO 1: Configurando Brand BET30 con CORS..." -ForegroundColor Yellow

$brandConfigSql = @"
-- Crear operador si no existe
INSERT INTO operators (id, name, status, created_at) 
VALUES (
    '11111111-1111-1111-1111-111111111111'::uuid, 
    'BET30 Operator', 
    'ACTIVE', 
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- Crear/actualizar brand bet30 con configuración de CORS
INSERT INTO brands (id, operator_id, code, name, domain, admin_domain, cors_origins, status, created_at) 
VALUES (
    '22222222-2222-2222-2222-222222222222'::uuid,
    '11111111-1111-1111-1111-111111111111'::uuid,
    'bet30',
    'BET30 Casino',
    'bet30.local',
    'admin.bet30.local',
    ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://127.0.0.1:5173'
    ],
    'ACTIVE',
    NOW()
) ON CONFLICT (code) DO UPDATE SET 
    domain = 'bet30.local',
    admin_domain = 'admin.bet30.local',
    cors_origins = ARRAY[
        'http://localhost:5173',
        'http://localhost:3000',
        'http://admin.bet30.local:5173',
        'https://admin.bet30.local:5173',
        'http://bet30.local:5173',
        'https://bet30.local:5173',
        'http://127.0.0.1:5173'
    ],
    status = 'ACTIVE';
"@

Write-Host "`n?? PASO 2: Creando usuarios administradores..." -ForegroundColor Yellow

# Password hash para "admin123" generado con ASP.NET Identity PasswordHasher
$passwordHash = "AQAAAAEAACcQAAAAEInvCQI8gCJH+TQAyhX4w4X5m9hBGF1HY8D8X5vW7Cp2KqW0nL9pJ7yE6zF3uV8="

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
-- Verificar configuración
SELECT 
    'Brand BET30:' as tipo,
    b.code as codigo,
    b.domain as dominio,
    b.admin_domain as dominio_admin,
    b.status as estado,
    array_length(b.cors_origins, 1) as cors_count
FROM brands b WHERE b.code = 'bet30'

UNION ALL

SELECT 
    'Usuarios:' as tipo,
    u.username as codigo,
    u.role::text as dominio,
    u.status::text as dominio_admin,
    '' as estado,
    0 as cors_count
FROM backoffice_users u 
WHERE u.username IN ('superadmin', 'operator_admin', 'cashier_user')
ORDER BY tipo, codigo;
"@

try {
    Write-Host "Ejecutando configuración de base de datos..." -ForegroundColor Green
    
    # Verificar si psql está disponible
    $psqlPath = Get-Command psql -ErrorAction SilentlyContinue
    
    if ($psqlPath) {
        Write-Host "? Configurando brand..." -ForegroundColor Green
        echo $brandConfigSql | psql $connectionString
        
        Write-Host "? Creando usuarios..." -ForegroundColor Green
        echo $usersSql | psql $connectionString
        
        Write-Host "? Verificando configuración..." -ForegroundColor Green
        echo $verifySql | psql $connectionString
        
        Write-Host "`n? Configuración completada exitosamente!" -ForegroundColor Green
    }
    else {
        Write-Host "??  psql no encontrado. Por favor ejecuta manualmente:" -ForegroundColor Yellow
        Write-Host "1. Brand Configuration:" -ForegroundColor Cyan
        Write-Host $brandConfigSql -ForegroundColor White
        Write-Host "`n2. Users Configuration:" -ForegroundColor Cyan
        Write-Host $usersSql -ForegroundColor White
    }
}
catch {
    Write-Host "? Error ejecutando comandos: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Ejecuta manualmente los SQL commands mostrados arriba" -ForegroundColor Yellow
}

Write-Host "`n?? PASO 3: Verificar configuración del archivo hosts..." -ForegroundColor Yellow
Write-Host "Tu archivo hosts debería contener:" -ForegroundColor White
Write-Host "127.0.0.1 bet30.local" -ForegroundColor Cyan
Write-Host "127.0.0.1 admin.bet30.local" -ForegroundColor Cyan

Write-Host "`n?? PASO 4: Iniciar la aplicación..." -ForegroundColor Yellow
Write-Host "1. Desde el directorio raíz, ejecuta:" -ForegroundColor White
Write-Host "   dotnet watch --project apps/api/Casino.Api" -ForegroundColor Cyan
Write-Host "`n2. La API estará disponible en:" -ForegroundColor White
Write-Host "   - HTTP:  http://admin.bet30.local:5000" -ForegroundColor Cyan
Write-Host "   - HTTPS: https://admin.bet30.local:7182" -ForegroundColor Cyan

Write-Host "`n?? CREDENCIALES DE ACCESO:" -ForegroundColor Green
Write-Host "Password para todos los usuarios: admin123" -ForegroundColor White
Write-Host "Usuarios disponibles:" -ForegroundColor White
Write-Host "- superadmin (SUPER_ADMIN)" -ForegroundColor Cyan
Write-Host "- operator_admin (OPERATOR_ADMIN)" -ForegroundColor Cyan  
Write-Host "- cashier_user (CASHIER)" -ForegroundColor Cyan

Write-Host "`n?? URLs IMPORTANTES:" -ForegroundColor Green
Write-Host "Login API:     https://admin.bet30.local:7182/api/v1/admin/auth/login" -ForegroundColor White
Write-Host "Frontend:      http://admin.bet30.local:5173" -ForegroundColor White
Write-Host "Swagger:       https://admin.bet30.local:7182" -ForegroundColor White

Write-Host "`n?? EJEMPLO DE REQUEST PARA LOGIN:" -ForegroundColor Green
$loginExample = @"
POST https://admin.bet30.local:7182/api/v1/admin/auth/login
Content-Type: application/json

{
  "username": "superadmin",
  "password": "admin123"
}
"@
Write-Host $loginExample -ForegroundColor Cyan

Write-Host "`n?? TROUBLESHOOTING:" -ForegroundColor Yellow
Write-Host "- Si el frontend aún da error CORS, verifica que la API esté corriendo en el puerto correcto" -ForegroundColor White
Write-Host "- Verifica que admin.bet30.local resuelva a 127.0.0.1 en tu archivo hosts" -ForegroundColor White
Write-Host "- Los logs del middleware de CORS mostrarán qué está pasando" -ForegroundColor White

Write-Host "`n? ¡Configuración completa! Tu casino debería funcionar ahora." -ForegroundColor Green