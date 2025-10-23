-- ============================================================================
-- Script de Verificación de Datos del Dashboard
-- ============================================================================
-- Ejecuta estas queries para diagnosticar por qué el dashboard muestra 0
-- ============================================================================

-- 1. VERIFICAR BALANCES DE BACKOFFICE USERS
-- ============================================================================
SELECT 
    "Id",
    "Username",
    "Role",
  "WalletBalance",
    "BrandId",
    "ParentAdminId",
    "ParentCashierId",
    "HierarchyLevel"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY "Role", "Username";

-- Total de balances por rol
SELECT 
  "Role",
    COUNT(*) as usuarios,
    SUM("WalletBalance") as balance_total
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
GROUP BY "Role";

-- ============================================================================
-- 2. VERIFICAR PLAYERS Y SUS BALANCES
-- ============================================================================
SELECT 
    p."Id",
  p."Username",
    p."Status",
    p."CreatedByUserId",
    p."BrandId",
    w."BalanceBigint"
FROM "Players" p
LEFT JOIN "Wallets" w ON w."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY p."Username";

-- Total de balances de players
SELECT 
    COUNT(*) as total_players,
  SUM(w."BalanceBigint") as balance_total,
    COUNT(CASE WHEN p."Status" = 'ACTIVE' THEN 1 END) as players_activos
FROM "Players" p
LEFT JOIN "Wallets" w ON w."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- 3. VERIFICAR JERARQUÍA (Quién creó a quién)
-- ============================================================================
SELECT 
    u."Username" as usuario,
    u."Role",
  u."HierarchyLevel",
  u."HierarchyPath",
    parent."Username" as parent_admin,
cashier_parent."Username" as parent_cashier,
    u."WalletBalance"
FROM "BackofficeUsers" u
LEFT JOIN "BackofficeUsers" parent ON u."ParentAdminId" = parent."Id"
LEFT JOIN "BackofficeUsers" cashier_parent ON u."ParentCashierId" = cashier_parent."Id"
WHERE u."BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY u."HierarchyLevel", u."Username";

-- ============================================================================
-- 4. VERIFICAR TRANSACCIONES (Cargas, Depósitos, Retiros)
-- ============================================================================
-- Transacciones de hoy
SELECT 
    "TransactionType",
 "FromUserType",
    "ToUserType",
    COUNT(*) as cantidad,
    SUM("Amount") as total
FROM "WalletTransactions"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
  AND "CreatedAt" >= CURRENT_DATE
GROUP BY "TransactionType", "FromUserType", "ToUserType"
ORDER BY "TransactionType";

-- Últimas 10 transacciones
SELECT 
    wt."Id",
    wt."TransactionType",
    wt."FromUserType",
    from_user."Username" as from_user,
    wt."ToUserType",
    to_user."Username" as to_user,
    wt."Amount",
    wt."CreatedAt"
FROM "WalletTransactions" wt
LEFT JOIN "BackofficeUsers" from_user ON wt."FromUserId" = from_user."Id" AND wt."FromUserType" = 'BACKOFFICE'
LEFT JOIN "BackofficeUsers" to_user ON wt."ToUserId" = to_user."Id" AND wt."ToUserType" = 'BACKOFFICE'
WHERE wt."BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY wt."CreatedAt" DESC
LIMIT 10;

-- ============================================================================
-- 5. VERIFICAR ACTIVIDAD DE CASINO (Ledger)
-- ============================================================================
-- Resumen de apuestas de hoy
SELECT 
    l."Reason",
    COUNT(*) as cantidad,
  SUM(l."DeltaBigint") as total
FROM "Ledger" l
JOIN "Players" p ON l."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND l."CreatedAt" >= CURRENT_DATE
GROUP BY l."Reason"
ORDER BY l."Reason";

-- Últimas 10 apuestas
SELECT 
    l."Id",
    l."Reason",
    p."Username" as player,
    l."DeltaBigint",
    l."GameCode",
    l."Provider",
    l."CreatedAt"
FROM "Ledger" l
JOIN "Players" p ON l."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY l."CreatedAt" DESC
LIMIT 10;

-- ============================================================================
-- 6. VERIFICAR SESIONES ACTIVAS
-- ============================================================================
SELECT 
  gs."Id",
    p."Username" as player,
    gs."GameCode",
    gs."Provider",
    gs."Status",
    gs."ExpiresAt",
    gs."CreatedAt"
FROM "GameSessions" gs
JOIN "Players" p ON gs."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND gs."Status" = 'OPEN'
ORDER BY gs."CreatedAt" DESC;

-- ============================================================================
-- 7. VERIFICAR COMISIONES ACUMULADAS
-- ============================================================================
SELECT 
ca."Id",
    bu."Username" as usuario,
    ca."BaseAmount",
    ca."CommissionRate",
  ca."CommissionAmount",
    ca."Settled",
    ca."CreatedAt"
FROM "CommissionAccruals" ca
JOIN "BackofficeUsers" bu ON ca."UserId" = bu."Id"
WHERE ca."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND NOT ca."Settled"
ORDER BY ca."CreatedAt" DESC
LIMIT 10;

-- ============================================================================
-- 8. RESUMEN COMPLETO PARA DASHBOARD
-- ============================================================================
-- Esta es la data que DEBERÍA aparecer en el dashboard

WITH backoffice_balances AS (
    SELECT 
        SUM(CASE WHEN "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN') THEN "WalletBalance" ELSE 0 END) as house_balance,
    SUM(CASE WHEN "Role" = 'CASHIER' THEN "WalletBalance" ELSE 0 END) as cashiers_balance,
    COUNT(CASE WHEN "Role" = 'CASHIER' THEN 1 END) as total_cashiers
    FROM "BackofficeUsers"
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
),
player_balances AS (
    SELECT 
        COALESCE(SUM(w."BalanceBigint"), 0) as players_balance,
 COUNT(p."Id") as total_players,
COUNT(CASE WHEN p."Status" = 'ACTIVE' THEN 1 END) as active_players
    FROM "Players" p
    LEFT JOIN "Wallets" w ON w."PlayerId" = p."Id"
    WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
),
today_transactions AS (
    SELECT 
        SUM(CASE WHEN "TransactionType" = 'TRANSFER' 
   AND "FromUserType" = 'BACKOFFICE' 
          AND "ToUserType" = 'PLAYER' 
      THEN "Amount" ELSE 0 END) as cargas,
        SUM(CASE WHEN "TransactionType" = 'MINT' 
    AND "ToUserType" = 'BACKOFFICE' 
               THEN "Amount" ELSE 0 END) as depositos,
        SUM(CASE WHEN "TransactionType" IN ('WITHDRAWAL', 'BURN') 
           THEN "Amount" ELSE 0 END) as retiros
    FROM "WalletTransactions"
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
      AND "CreatedAt" >= CURRENT_DATE
),
today_casino AS (
    SELECT 
        SUM(CASE WHEN l."Reason" = 'BET' THEN l."DeltaBigint" ELSE 0 END) as jugado,
        SUM(CASE WHEN l."Reason" = 'WIN' THEN l."DeltaBigint" ELSE 0 END) as pagado
 FROM "Ledger" l
    JOIN "Players" p ON l."PlayerId" = p."Id"
    WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
AND l."CreatedAt" >= CURRENT_DATE
)
SELECT 
    '=== FINANZAS ===' as seccion,
    (bb.house_balance + bb.cashiers_balance + pb.players_balance) as fichas_total,
  bb.house_balance as house_balance,
    bb.cashiers_balance as cashiers_balance,
 pb.players_balance as players_balance,
tt.cargas,
    tt.depositos,
    tt.retiros,
    '=== USUARIOS ===' as usuarios_seccion,
    pb.total_players,
    pb.active_players,
    bb.total_cashiers,
    '=== CASINO ===' as casino_seccion,
    tc.jugado,
    tc.pagado,
    (tc.jugado - tc.pagado) as netwin
FROM backoffice_balances bb
CROSS JOIN player_balances pb
CROSS JOIN today_transactions tt
CROSS JOIN today_casino tc;

-- ============================================================================
-- 9. DIAGNÓSTICO: ¿Por qué está en 0?
-- ============================================================================
-- Esta query te dirá exactamente qué está faltando

SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM "BackofficeUsers" WHERE "BrandId" = '11111111-1111-1111-1111-111111111111') = 0 
        THEN '? NO HAY USUARIOS DE BACKOFFICE'
        ELSE '? Usuarios de backoffice encontrados'
  END as backoffice_users,
    
    CASE 
        WHEN (SELECT SUM("WalletBalance") FROM "BackofficeUsers" WHERE "BrandId" = '11111111-1111-1111-1111-111111111111') = 0 
      THEN '? BALANCES DE BACKOFFICE EN 0'
        ELSE '? Backoffice tiene balance'
    END as backoffice_balance,
    
    CASE 
     WHEN (SELECT COUNT(*) FROM "Players" WHERE "BrandId" = '11111111-1111-1111-1111-111111111111') = 0 
        THEN '? NO HAY PLAYERS'
        ELSE '? Players encontrados'
    END as players,
    
    CASE 
        WHEN (SELECT SUM("BalanceBigint") FROM "Wallets" w JOIN "Players" p ON w."PlayerId" = p."Id" WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111') = 0 
 THEN '? BALANCES DE PLAYERS EN 0'
        ELSE '? Players tienen balance'
    END as player_balance,
 
    CASE 
        WHEN (SELECT COUNT(*) FROM "WalletTransactions" WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' AND "CreatedAt" >= CURRENT_DATE) = 0 
   THEN '? NO HAY TRANSACCIONES HOY'
        ELSE '? Transacciones de hoy encontradas'
    END as transactions_today,
    
    CASE 
        WHEN (SELECT COUNT(*) FROM "Ledger" l JOIN "Players" p ON l."PlayerId" = p."Id" WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111' AND l."CreatedAt" >= CURRENT_DATE) = 0 
        THEN '? NO HAY APUESTAS HOY'
     ELSE '? Apuestas de hoy encontradas'
    END as ledger_today;

-- ============================================================================
-- FIN DEL SCRIPT DE VERIFICACIÓN
-- ============================================================================
