-- ============================================================================
-- Script para crear usuario SUPER_ADMIN
-- ============================================================================
-- Username: superadmin
-- Password: password
-- 
-- IMPORTANTE: El hash de la contraseña fue generado usando ASP.NET Core Identity PasswordHasher
-- compatible con el PasswordHasher<object> usado en PasswordService.cs
--
-- Hash generado con: dotnet run --project scripts/HashGenerator/HashGenerator.csproj
--
-- EJECUCIÓN:
--   psql -h localhost -U postgres -d casino_platform -f scripts/create-superadmin.sql
--
-- O desde pgAdmin/DBeaver: Copiar y pegar el contenido de este archivo
-- ============================================================================

BEGIN;

-- Eliminar usuario existente si existe (opcional, comentar si no deseas eliminar)
DELETE FROM "BackofficeUsers" WHERE "Username" = 'superadmin';

-- Insertar SUPER_ADMIN
-- Hash generado para password "password" usando PasswordHasher<object>
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
    'superadmin',
    'AQAAAAEAACcQAAAAEBKUc5OV3dSOrNs7WQkmOf8id1ddhc4spoaR7E74VWNZoj6kOEoKwLLFxFZN/VF+Qg==',
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
    "BrandId",
    "CreatedAt"
FROM "BackofficeUsers"
WHERE "Username" = 'superadmin';

COMMIT;

-- ============================================================================
-- CREDENCIALES CREADAS
-- ============================================================================
-- Username: superadmin
-- Password: password
-- Role: SUPER_ADMIN
-- ============================================================================

-- ============================================================================
-- NOTAS SOBRE SEGURIDAD
-- ============================================================================
-- 1. Este script es para desarrollo/pruebas. En producción:
--    - Usa una contraseña fuerte y única
--    - Cambia la contraseña inmediatamente después del primer login
--
-- 2. Para regenerar el hash con una contraseña diferente, ejecuta:
--    dotnet run --project scripts/HashGenerator/HashGenerator.csproj TuContraseña
--
-- 3. El HashGenerator usa el mismo PasswordHasher<object> que PasswordService.cs
-- ============================================================================
