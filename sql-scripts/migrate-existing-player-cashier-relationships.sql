-- Script para migrar las relaciones existentes de jugadores-cajeros
-- Actualiza el campo CreatedByUserId basado en la tabla CashierPlayer

-- 1. Actualizar jugadores que ya están asignados a cajeros
UPDATE "Players" 
SET "CreatedByUserId" = cp."CashierId"
FROM "CashierPlayers" cp
WHERE "Players"."Id" = cp."PlayerId"
  AND "Players"."CreatedByUserId" IS NULL;

-- 2. Verificar cuántos registros se actualizaron
SELECT 
    COUNT(*) as total_players,
    COUNT("CreatedByUserId") as players_with_creator,
    COUNT(*) - COUNT("CreatedByUserId") as players_without_creator
FROM "Players";

-- 3. Verificar las relaciones Player -> BackofficeUser
SELECT 
    b."Name" as brand_name,
    bu."Username" as creator_username,
    bu."Role" as creator_role,
    COUNT(p."Id") as players_created
FROM "Players" p
JOIN "BackofficeUsers" bu ON p."CreatedByUserId" = bu."Id"
JOIN "Brands" b ON p."BrandId" = b."Id"
GROUP BY b."Name", bu."Username", bu."Role"
ORDER BY b."Name", bu."Username";

-- 4. Verificar BackofficeUsers sin CreatedByUserId (usuarios originales del sistema)
SELECT 
    bu."Username",
    bu."Role",
    b."Name" as brand_name,
    bu."CreatedAt"
FROM "BackofficeUsers" bu
LEFT JOIN "Brands" b ON bu."BrandId" = b."Id"
WHERE bu."CreatedByUserId" IS NULL
ORDER BY bu."CreatedAt";

-- 5. Estadísticas generales
SELECT 
    'BackofficeUsers' as table_name,
    COUNT(*) as total_records,
    COUNT("CreatedByUserId") as records_with_creator,
    COUNT(*) - COUNT("CreatedByUserId") as records_without_creator
FROM "BackofficeUsers"
UNION ALL
SELECT 
    'Players' as table_name,
    COUNT(*) as total_records,
    COUNT("CreatedByUserId") as records_with_creator,
    COUNT(*) - COUNT("CreatedByUserId") as records_without_creator
FROM "Players";