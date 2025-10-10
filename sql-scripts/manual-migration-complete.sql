-- ============================================================================
-- MIGRACIÓN MANUAL - ELIMINACIÓN DEL SISTEMA DE OPERADORES
-- ============================================================================
-- Aplica esta migración MANUALMENTE en la base de datos
-- Esto es una alternativa a las migraciones EF Core que tienen errores
-- ============================================================================

-- Paso 1: Backup de seguridad (EJECUTA ESTO ANTES DE NADA)
-- CREATE TABLE BackofficeUsers_Backup AS SELECT * FROM "BackofficeUsers";
-- CREATE TABLE Brands_Backup AS SELECT * FROM "Brands";
-- CREATE TABLE Ledger_Backup AS SELECT * FROM "Ledger";
-- CREATE TABLE Operators_Backup AS SELECT * FROM "Operators";

-- Paso 2: Mostrar estado actual
SELECT 'Estado actual - Operadores:' as info;
SELECT "Id", "Name", "Status" FROM "Operators" ORDER BY "Name";

SELECT 'Estado actual - BackofficeUsers con OperatorId:' as info;
SELECT 
    u."Id", 
    u."Username", 
    u."Role", 
    u."OperatorId", 
    o."Name" as "OperatorName"
FROM "BackofficeUsers" u
LEFT JOIN "Operators" o ON u."OperatorId" = o."Id"
ORDER BY u."Username";

-- Paso 3: Agregar nueva columna BrandId a BackofficeUsers
ALTER TABLE "BackofficeUsers" ADD COLUMN "BrandId" uuid;

-- Paso 4: Crear índice y foreign key para BrandId
CREATE INDEX "IX_BackofficeUsers_BrandId" ON "BackofficeUsers" ("BrandId");

ALTER TABLE "BackofficeUsers" 
ADD CONSTRAINT "FK_BackofficeUsers_Brands_BrandId" 
FOREIGN KEY ("BrandId") REFERENCES "Brands"("Id");

-- Paso 5: Migrar datos - Asignar usuarios a brands basado en su operador
-- Para usuarios BRAND_ADMIN y CASHIER, asignar al primer brand activo de su operador
UPDATE "BackofficeUsers" 
SET "BrandId" = (
    SELECT b."Id" 
    FROM "Brands" b 
    WHERE b."OperatorId" = "BackofficeUsers"."OperatorId" 
    AND b."Status" = 'ACTIVE'
    ORDER BY b."CreatedAt" ASC
    LIMIT 1
)
WHERE "Role" IN ('BRAND_ADMIN', 'CASHIER') 
AND "OperatorId" IS NOT NULL;

-- Paso 6: Verificar migración de datos
SELECT 'Verificación - Usuarios sin brand asignado:' as info;
SELECT 
    u."Id", 
    u."Username", 
    u."Role",
    u."OperatorId",
    u."BrandId"
FROM "BackofficeUsers" u 
WHERE u."Role" != 'SUPER_ADMIN' AND u."BrandId" IS NULL;

-- Si hay usuarios sin brand, asígnalos manualmente:
-- UPDATE "BackofficeUsers" SET "BrandId" = 'GUID_DEL_BRAND' WHERE "Id" = 'GUID_DEL_USUARIO';

-- Paso 7: Verificar integridad de jerarquía de cashiers
SELECT 'Verificación - Jerarquía de cashiers:' as info;
SELECT 
    c."Id" as cashier_id,
    c."Username" as cashier_username,
    c."BrandId" as cashier_brand,
    p."Id" as parent_id,
    p."Username" as parent_username,
    p."BrandId" as parent_brand,
    CASE 
        WHEN c."BrandId" = p."BrandId" THEN 'OK' 
        WHEN c."BrandId" IS NULL OR p."BrandId" IS NULL THEN 'NULL_BRAND'
        ELSE 'BRAND_MISMATCH' 
    END as status
FROM "BackofficeUsers" c
LEFT JOIN "BackofficeUsers" p ON c."ParentCashierId" = p."Id"
WHERE c."Role" = 'CASHIER' AND c."ParentCashierId" IS NOT NULL;

-- Si hay mismatches de brand en la jerarquía, corregir:
-- UPDATE "BackofficeUsers" c SET "BrandId" = p."BrandId"
-- FROM "BackofficeUsers" p
-- WHERE c."ParentCashierId" = p."Id" AND c."BrandId" != p."BrandId";

-- Paso 8: Eliminar foreign keys que referencian Operators
ALTER TABLE "BackofficeUsers" DROP CONSTRAINT IF EXISTS "FK_BackofficeUsers_Operators_OperatorId";
ALTER TABLE "Brands" DROP CONSTRAINT IF EXISTS "FK_Brands_Operators_OperatorId";
ALTER TABLE "Ledger" DROP CONSTRAINT IF EXISTS "FK_Ledger_Operators_OperatorId";

-- Paso 9: Eliminar índices relacionados con OperatorId
DROP INDEX IF EXISTS "IX_BackofficeUsers_OperatorId";
DROP INDEX IF EXISTS "IX_Brands_OperatorId";
DROP INDEX IF EXISTS "IX_Ledger_OperatorId";

-- Paso 10: Eliminar tabla Operators
DROP TABLE "Operators";

-- Paso 11: Eliminar columnas OperatorId
ALTER TABLE "BackofficeUsers" DROP COLUMN "OperatorId";
ALTER TABLE "Brands" DROP COLUMN "OperatorId";
ALTER TABLE "Ledger" DROP COLUMN "OperatorId";

-- Paso 12: Verificación final
SELECT 'Verificación final - Estructura de BackofficeUsers:' as info;
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'BackofficeUsers' 
AND table_schema = 'public'
ORDER BY ordinal_position;

SELECT 'Verificación final - Usuarios por brand:' as info;
SELECT 
    COALESCE(b."Code", 'NO_BRAND') as brand_code,
    COALESCE(b."Name", 'NO_BRAND') as brand_name,
    u."Role",
    COUNT(*) as user_count
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
GROUP BY b."Code", b."Name", u."Role"
ORDER BY brand_code, u."Role";

SELECT 'Verificación final - Referencias huérfanas:' as info;
SELECT COUNT(*) as orphaned_references
FROM "BackofficeUsers" u
LEFT JOIN "Brands" b ON u."BrandId" = b."Id"
WHERE u."BrandId" IS NOT NULL AND b."Id" IS NULL;

-- Paso 13: Actualizar estadísticas
ANALYZE "BackofficeUsers";
ANALYZE "Brands";
ANALYZE "Ledger";

-- Paso 14: Crear auditoría de la migración
INSERT INTO "BackofficeAudits" (
    "Id", 
    "UserId", 
    "Action", 
    "TargetType", 
    "TargetId", 
    "Meta", 
    "CreatedAt"
) VALUES (
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000000', -- System user
    'MANUAL_MIGRATION',
    'System',
    'operator_removal_manual',
    json_build_object(
        'description', 'Manual migration - Removed operator system',
        'migration_date', NOW(),
        'tables_affected', ARRAY['BackofficeUsers', 'Brands', 'Ledger', 'Operators'],
        'method', 'manual_sql'
    )::jsonb,
    NOW()
);

SELECT 'MIGRACIÓN COMPLETADA EXITOSAMENTE!' as resultado;
SELECT 'Recuerda actualizar el código de la aplicación para eliminar referencias a operators.' as nota;