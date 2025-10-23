-- ============================================================================
-- Fix para Jerarquía Rota en Localhost Development
-- ============================================================================
-- Este script corrige ParentAdminId y HierarchyLevel para los usuarios
-- existentes en el brand LOCALHOST_DEV
-- ============================================================================

BEGIN;

-- 1. Corregir localadmin (debe reportar a superadmin)
UPDATE "BackofficeUsers"
SET 
    "ParentAdminId" = 'a8e3149b-9e79-4e36-88d9-6ca032420607', -- superadmin
    "HierarchyLevel" = 1,
    "HierarchyPath" = 'a8e3149b-9e79-4e36-88d9-6ca032420607'
WHERE "Id" = 'ea3080a9-64d6-479c-9500-73730333e3a5' -- localadmin
  AND "BrandId" = '11111111-1111-1111-1111-111111111111';

-- 2. Corregir localcajero (debe reportar a localadmin)
UPDATE "BackofficeUsers"
SET 
    "ParentAdminId" = 'ea3080a9-64d6-479c-9500-73730333e3a5', -- localadmin
    "HierarchyLevel" = 2,
    "HierarchyPath" = 'a8e3149b-9e79-4e36-88d9-6ca032420607/ea3080a9-64d6-479c-9500-73730333e3a5'
WHERE "Id" = '85943639-6954-43f7-bc0a-b620bd390cd1' -- localcajero
  AND "BrandId" = '11111111-1111-1111-1111-111111111111';

-- 3. Corregir localcajero2 (debe reportar a localcajero)
UPDATE "BackofficeUsers"
SET 
    "ParentAdminId" = '85943639-6954-43f7-bc0a-b620bd390cd1', -- localcajero
    "HierarchyLevel" = 3,
    "HierarchyPath" = 'a8e3149b-9e79-4e36-88d9-6ca032420607/ea3080a9-64d6-479c-9500-73730333e3a5/85943639-6954-43f7-bc0a-b620bd390cd1'
WHERE "Id" = '2ba7af1a-20e3-406f-b29d-57d9449348e2' -- localcajero2
  AND "BrandId" = '11111111-1111-1111-1111-111111111111';

-- 4. Establecer comisión para localcajero (si no la tiene)
UPDATE "BackofficeUsers"
SET "CommissionPercent" = 10
WHERE "Id" = '85943639-6954-43f7-bc0a-b620bd390cd1' -- localcajero
  AND "CommissionPercent" = 0;

-- 5. Verificación
SELECT 
    "Username",
    "Role",
 "HierarchyLevel",
    "ParentAdminId",
    (SELECT "Username" FROM "BackofficeUsers" p WHERE p."Id" = "BackofficeUsers"."ParentAdminId") as "ParentUsername",
    "CommissionPercent",
    "WalletBalance"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
   OR "Role" = 'SUPER_ADMIN'
ORDER BY "HierarchyLevel", "Username";

COMMIT;
