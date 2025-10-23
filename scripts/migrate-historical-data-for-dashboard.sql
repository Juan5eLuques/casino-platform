-- ============================================================================
-- Script de Migración de Datos Históricos para Dashboard
-- ============================================================================
-- Este script genera transacciones MINT/TRANSFER iniciales para balances
-- existentes que no tienen registros en WalletTransactions.
--
-- PROPÓSITO:
-- - Permitir que el Dashboard muestre datos correctos
-- - Establecer trazabilidad retroactiva
-- - Corregir Players.CreatedByUserId NULL
-- ============================================================================

\set ON_ERROR_STOP on
\set brand_id '11111111-1111-1111-1111-111111111111'

\echo '============================================================================'
\echo 'MIGRACIÓN DE DATOS HISTÓRICOS PARA DASHBOARD'
\echo '============================================================================'
\echo 'Brand ID:' :brand_id
\echo ''

-- ============================================================================
-- PASO 1: Diagnóstico Pre-Migración
-- ============================================================================

\echo '============================================================================'
\echo 'PASO 1: DIAGNÓSTICO PRE-MIGRACIÓN'
\echo '============================================================================'
\echo ''

\echo '1.1) Balances Actuales'
\echo '----------------------------------------------------------------------'

SELECT 
    'SUPER_ADMIN' as tipo,
    COUNT(*) as usuarios,
  SUM("WalletBalance") as balance_total,
    SUM(CASE WHEN EXISTS (
     SELECT 1 FROM "WalletTransactions" wt 
        WHERE wt."ToUserId" = "BackofficeUsers"."Id"
    ) THEN 1 ELSE 0 END) as con_transacciones
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id' AND "Role" = 'SUPER_ADMIN'

UNION ALL

SELECT 
    'BRAND_ADMIN' as tipo,
  COUNT(*),
    SUM("WalletBalance"),
    SUM(CASE WHEN EXISTS (SELECT 1 FROM "WalletTransactions" wt WHERE wt."ToUserId" = "BackofficeUsers"."Id") THEN 1 ELSE 0 END)
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id' AND "Role" = 'BRAND_ADMIN'

UNION ALL

SELECT 
  'CASHIER' as tipo,
    COUNT(*),
    SUM("WalletBalance"),
    SUM(CASE WHEN EXISTS (SELECT 1 FROM "WalletTransactions" wt WHERE wt."ToUserId" = "BackofficeUsers"."Id") THEN 1 ELSE 0 END)
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id' AND "Role" = 'CASHIER'

UNION ALL

SELECT 
    'PLAYERS' as tipo,
    COUNT(*),
    SUM("WalletBalance"),
    SUM(CASE WHEN EXISTS (SELECT 1 FROM "WalletTransactions" wt WHERE wt."ToUserId" = "Players"."Id") THEN 1 ELSE 0 END)
FROM "Players"
WHERE "BrandId" = :'brand_id';

\echo ''
\echo '1.2) Players sin CreatedByUserId'
\echo '----------------------------------------------------------------------'

SELECT 
    COUNT(*) as players_sin_creator,
    SUM("WalletBalance") as balance_total_sin_creator
FROM "Players"
WHERE "BrandId" = :'brand_id'
  AND "CreatedByUserId" IS NULL;

\echo ''
\echo '1.3) Transacciones Existentes'
\echo '----------------------------------------------------------------------'

SELECT 
    CASE "TransactionType"
      WHEN 0 THEN 'MINT'
        WHEN 1 THEN 'TRANSFER'
     WHEN 2 THEN 'BET'
   WHEN 3 THEN 'WIN'
      WHEN 4 THEN 'ROLLBACK'
        WHEN 5 THEN 'DEPOSIT'
        WHEN 6 THEN 'WITHDRAWAL'
     WHEN 7 THEN 'BONUS'
        WHEN 8 THEN 'ADJUSTMENT'
    ELSE 'UNKNOWN'
    END as tipo,
    COUNT(*) as cantidad,
    SUM("Amount") as total
FROM "WalletTransactions"
WHERE "BrandId" = :'brand_id'
GROUP BY "TransactionType"
ORDER BY "TransactionType";

-- ============================================================================
-- PASO 2: Insertar Transacciones MINT para SUPER_ADMIN/BRAND_ADMIN
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'PASO 2: INSERTAR MINT PARA SUPER_ADMIN/BRAND_ADMIN'
\echo '============================================================================'
\echo ''

WITH inserted AS (
    INSERT INTO "WalletTransactions" (
  "Id",
        "BrandId",
        "FromUserId",
        "FromUserType",
        "ToUserId",
        "ToUserType",
        "Amount",
        "TransactionType",
        "PreviousBalanceFrom",
  "NewBalanceFrom",
        "PreviousBalanceTo",
    "NewBalanceTo",
        "Description",
        "CreatedByUserId",
        "CreatedByRole",
        "IdempotencyKey",
     "CreatedAt"
    )
    SELECT 
        gen_random_uuid(),
        "BrandId",
        NULL, -- From: Sistema (MINT)
    NULL,
 "Id", -- To: Admin
        'BACKOFFICE',
 "WalletBalance",
        0, -- MINT
        NULL,
        NULL,
  0, -- Previous balance 0
    "WalletBalance",
        'Initial balance - Historical migration',
   "Id",
        "Role"::text,
        'migration-mint-admin-' || "Id"::text,
        "CreatedAt"
    FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id'
      AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN')
      AND "WalletBalance" > 0
      AND NOT EXISTS (
          SELECT 1 FROM "WalletTransactions" wt 
          WHERE wt."ToUserId" = "BackofficeUsers"."Id"
 )
    RETURNING *
)
SELECT 
    COUNT(*) as transacciones_mint_creadas,
    SUM("Amount") as monto_total
FROM inserted;

-- ============================================================================
-- PASO 3: Insertar Transacciones TRANSFER para CASHIERS
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'PASO 3: INSERTAR TRANSFER PARA CASHIERS'
\echo '============================================================================'
\echo ''

WITH super_admin AS (
    SELECT "Id", "BrandId" 
    FROM "BackofficeUsers" 
    WHERE "BrandId" = :'brand_id' 
   AND "Role" = 'SUPER_ADMIN' 
    ORDER BY "CreatedAt"
    LIMIT 1
),
inserted AS (
    INSERT INTO "WalletTransactions" (
   "Id",
   "BrandId",
        "FromUserId",
 "FromUserType",
        "ToUserId",
        "ToUserType",
        "Amount",
        "TransactionType",
  "PreviousBalanceFrom",
        "NewBalanceFrom",
        "PreviousBalanceTo",
        "NewBalanceTo",
        "Description",
     "CreatedByUserId",
        "CreatedByRole",
     "IdempotencyKey",
        "CreatedAt"
    )
    SELECT 
        gen_random_uuid(),
        bu."BrandId",
        sa."Id", -- From: SUPER_ADMIN
        'BACKOFFICE',
        bu."Id", -- To: CASHIER
        'BACKOFFICE',
        bu."WalletBalance",
        1, -- TRANSFER
 0,
      0,
     0,
    bu."WalletBalance",
        'Initial balance - Historical migration from SUPER_ADMIN',
        sa."Id",
      'SUPER_ADMIN',
        'migration-transfer-cashier-' || bu."Id"::text,
 bu."CreatedAt"
    FROM "BackofficeUsers" bu
    CROSS JOIN super_admin sa
    WHERE bu."BrandId" = :'brand_id'
      AND bu."Role" = 'CASHIER'
      AND bu."WalletBalance" > 0
 AND NOT EXISTS (
    SELECT 1 FROM "WalletTransactions" wt 
          WHERE wt."ToUserId" = bu."Id"
      )
    RETURNING *
)
SELECT 
    COUNT(*) as transacciones_transfer_cashier_creadas,
    SUM("Amount") as monto_total
FROM inserted;

-- ============================================================================
-- PASO 4: Establecer CreatedByUserId en Players
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'PASO 4: ESTABLECER CreatedByUserId EN PLAYERS'
\echo '============================================================================'
\echo ''

-- Opción 1: Distribuir equitativamente entre cashiers disponibles
WITH cashiers AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt") as rn
    FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id'
      AND "Role" = 'CASHIER'
),
players_to_update AS (
    SELECT 
  p."Id" as player_id,
        ROW_NUMBER() OVER (ORDER BY p."CreatedAt") as player_rn,
        (SELECT COUNT(*) FROM cashiers) as total_cashiers
    FROM "Players" p
    WHERE p."BrandId" = :'brand_id'
      AND p."CreatedByUserId" IS NULL
),
assignments AS (
    SELECT 
        ptu.player_id,
        c."Id" as cashier_id
    FROM players_to_update ptu
    JOIN cashiers c ON ((ptu.player_rn - 1) % ptu.total_cashiers) + 1 = c.rn
)
UPDATE "Players" p
SET "CreatedByUserId" = a.cashier_id
FROM assignments a
WHERE p."Id" = a.player_id
RETURNING p."Id";

SELECT 
    COUNT(*) as players_actualizados
FROM "Players"
WHERE "BrandId" = :'brand_id'
  AND "CreatedByUserId" IS NOT NULL;

-- ============================================================================
-- PASO 5: Insertar Transacciones TRANSFER para PLAYERS
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'PASO 5: INSERTAR TRANSFER PARA PLAYERS'
\echo '============================================================================'
\echo ''

WITH inserted AS (
    INSERT INTO "WalletTransactions" (
        "Id",
    "BrandId",
   "FromUserId",
        "FromUserType",
        "ToUserId",
        "ToUserType",
        "Amount",
        "TransactionType",
        "PreviousBalanceFrom",
  "NewBalanceFrom",
        "PreviousBalanceTo",
   "NewBalanceTo",
        "Description",
 "CreatedByUserId",
  "CreatedByRole",
        "IdempotencyKey",
        "CreatedAt"
    )
    SELECT 
        gen_random_uuid(),
    p."BrandId",
    p."CreatedByUserId", -- From: CASHIER asignado
        'BACKOFFICE',
 p."Id", -- To: PLAYER
        'PLAYER',
        p."WalletBalance",
        1, -- TRANSFER
        0,
        0,
        0,
        p."WalletBalance",
        'Initial balance - Historical migration from CASHIER',
        p."CreatedByUserId",
        'CASHIER',
        'migration-transfer-player-' || p."Id"::text,
        p."CreatedAt"
    FROM "Players" p
    WHERE p."BrandId" = :'brand_id'
      AND p."WalletBalance" > 0
      AND p."CreatedByUserId" IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM "WalletTransactions" wt 
          WHERE wt."ToUserId" = p."Id"
      )
    RETURNING *
)
SELECT 
COUNT(*) as transacciones_transfer_player_creadas,
    SUM("Amount") as monto_total
FROM inserted;

-- ============================================================================
-- PASO 6: Verificación Post-Migración
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'PASO 6: VERIFICACIÓN POST-MIGRACIÓN'
\echo '============================================================================'
\echo ''

\echo '6.1) Transacciones Creadas'
\echo '----------------------------------------------------------------------'

SELECT 
    CASE "TransactionType"
        WHEN 0 THEN 'MINT'
        WHEN 1 THEN 'TRANSFER'
        WHEN 2 THEN 'BET'
    WHEN 3 THEN 'WIN'
        WHEN 4 THEN 'ROLLBACK'
      WHEN 5 THEN 'DEPOSIT'
        WHEN 6 THEN 'WITHDRAWAL'
        WHEN 7 THEN 'BONUS'
 WHEN 8 THEN 'ADJUSTMENT'
        ELSE 'UNKNOWN'
    END as tipo,
    COUNT(*) as cantidad,
    SUM("Amount") as total
FROM "WalletTransactions"
WHERE "BrandId" = :'brand_id'
GROUP BY "TransactionType"
ORDER BY "TransactionType";

\echo ''
\echo '6.2) Balance de Transacciones vs Balances Actuales'
\echo '----------------------------------------------------------------------'

WITH transaction_balances AS (
    SELECT 
        'BACKOFFICE' as tipo,
        SUM(CASE 
    WHEN "ToUserType" = 'BACKOFFICE' THEN "Amount"
            WHEN "FromUserType" = 'BACKOFFICE' THEN -"Amount"
    ELSE 0
        END) as balance_transacciones
FROM "WalletTransactions"
    WHERE "BrandId" = :'brand_id'
      AND ("ToUserType" = 'BACKOFFICE' OR "FromUserType" = 'BACKOFFICE')
    
    UNION ALL
    
    SELECT 
  'PLAYER' as tipo,
        SUM(CASE 
            WHEN "ToUserType" = 'PLAYER' THEN "Amount"
     WHEN "FromUserType" = 'PLAYER' THEN -"Amount"
  ELSE 0
        END)
FROM "WalletTransactions"
    WHERE "BrandId" = :'brand_id'
      AND ("ToUserType" = 'PLAYER' OR "FromUserType" = 'PLAYER')
),
current_balances AS (
    SELECT 
        'BACKOFFICE' as tipo,
        SUM("WalletBalance") as balance_actual
    FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id'
    
  UNION ALL
    
    SELECT 
'PLAYER' as tipo,
        SUM("WalletBalance")
    FROM "Players"
    WHERE "BrandId" = :'brand_id'
)
SELECT 
    cb.tipo,
    cb.balance_actual,
    tb.balance_transacciones,
    cb.balance_actual - tb.balance_transacciones as diferencia,
    CASE 
        WHEN ABS(cb.balance_actual - tb.balance_transacciones) < 0.01 THEN '? OK'
        ELSE '?? DISCREPANCIA'
    END as estado
FROM current_balances cb
JOIN transaction_balances tb ON cb.tipo = tb.tipo;

\echo ''
\echo '6.3) Players con CreatedByUserId'
\echo '----------------------------------------------------------------------'

SELECT 
COUNT(*) as total_players,
    SUM(CASE WHEN "CreatedByUserId" IS NOT NULL THEN 1 ELSE 0 END) as con_creator,
    SUM(CASE WHEN "CreatedByUserId" IS NULL THEN 1 ELSE 0 END) as sin_creator,
    ROUND(100.0 * SUM(CASE WHEN "CreatedByUserId" IS NOT NULL THEN 1 ELSE 0 END) / COUNT(*), 2) as porcentaje_con_creator
FROM "Players"
WHERE "BrandId" = :'brand_id';

\echo ''
\echo '6.4) Validación de Jerarquía'
\echo '----------------------------------------------------------------------'

SELECT 
    bu."Username" as cajero,
 COUNT(p."Id") as players_asignados,
    SUM(p."WalletBalance") as balance_total_players,
    bu."WalletBalance" as balance_cajero
FROM "BackofficeUsers" bu
LEFT JOIN "Players" p ON p."CreatedByUserId" = bu."Id"
WHERE bu."BrandId" = :'brand_id'
  AND bu."Role" = 'CASHIER'
GROUP BY bu."Id", bu."Username", bu."WalletBalance"
ORDER BY bu."Username";

\echo ''
\echo '============================================================================'
\echo 'MIGRACIÓN COMPLETADA'
\echo '============================================================================'
\echo 'Ahora puedes verificar el dashboard con:'
\echo 'GET /api/v1/admin/dashboard/overview?scope=TREE'
\echo '============================================================================'
