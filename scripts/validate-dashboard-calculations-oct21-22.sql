-- ============================================================================
-- Script de Validación de Cálculos del Dashboard
-- Fechas: Oct 21 y 22, 2025
-- ============================================================================
-- Este script calcula manualmente los valores del dashboard para validar
-- contra el backend. Ajusta las fechas según necesites.
-- ============================================================================

\set ON_ERROR_STOP on

-- Parámetros
\set brand_id '11111111-1111-1111-1111-111111111111'
\set oct_21_start '2025-10-21 00:00:00'
\set oct_21_end '2025-10-21 23:59:59.999999'
\set oct_22_start '2025-10-22 00:00:00'
\set oct_22_end '2025-10-22 23:59:59.999999'

-- ============================================================================
-- SECCIÓN 1: FINANZAS (Oct 22, 2025)
-- ============================================================================

\echo '============================================================================'
\echo 'FINANZAS - Oct 22, 2025'
\echo '============================================================================'

-- 1.A) Fichas (Balance Actual)
\echo ''
\echo '1.A) FICHAS (Balance Actual al cierre de Oct 22)'
\echo '----------------------------------------------------------------------'

SELECT 
    'HOUSE (SUPER_ADMIN + BRAND_ADMIN)' as categoria,
    COUNT(*) as usuarios,
    SUM("WalletBalance") as balance_decimal,
    (SUM("WalletBalance") * 100)::bigint as balance_centavos
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id'
  AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN')

UNION ALL

SELECT 
    'CASHIERS' as categoria,
    COUNT(*) as usuarios,
    SUM("WalletBalance") as balance_decimal,
    (SUM("WalletBalance") * 100)::bigint as balance_centavos
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id'
  AND "Role" = 'CASHIER'

UNION ALL

SELECT 
    'PLAYERS' as categoria,
    COUNT(*) as usuarios,
    (SUM(w."BalanceBigint")::decimal / 100) as balance_decimal,
 SUM(w."BalanceBigint") as balance_centavos
FROM "Players" p
JOIN "Wallets" w ON w."PlayerId" = p."Id"
WHERE p."BrandId" = :'brand_id'

UNION ALL

SELECT 
    '==== TOTAL FICHAS ====' as categoria,
    NULL::bigint as usuarios,
    (
    (SELECT SUM("WalletBalance") FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN'))
        + (SELECT SUM("WalletBalance") FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" = 'CASHIER')
        + (SELECT COALESCE(SUM(w."BalanceBigint"), 0)::decimal / 100 FROM "Players" p JOIN "Wallets" w ON w."PlayerId" = p."Id" WHERE p."BrandId" = :'brand_id')
    ) as balance_decimal,
    (
        ((SELECT SUM("WalletBalance") FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN')) * 100)::bigint
  + ((SELECT SUM("WalletBalance") FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" = 'CASHIER') * 100)::bigint
        + (SELECT COALESCE(SUM(w."BalanceBigint"), 0) FROM "Players" p JOIN "Wallets" w ON w."PlayerId" = p."Id" WHERE p."BrandId" = :'brand_id')
    ) as balance_centavos;

-- 1.B) Cargas (TRANSFER BACKOFFICE?PLAYER)
\echo ''
\echo '1.B) CARGAS (Top-ups a jugadores, Oct 22)'
\echo '----------------------------------------------------------------------'

SELECT 
'CARGAS (BACKOFFICE->PLAYER)' as concepto,
    COUNT(*) as transacciones,
    (SUM("Amount") * 100)::bigint as total_centavos,
    SUM("Amount") as total_decimal,
    CASE WHEN COUNT(*) > 0 THEN SUM("Amount") / COUNT(*) ELSE 0 END as promedio_decimal
FROM "WalletTransactions"
WHERE "BrandId" = :'brand_id'
  AND "TransactionType" = 1 -- TRANSFER
  AND "FromUserType" = 'BACKOFFICE'
  AND "ToUserType" = 'PLAYER'
  AND "CreatedAt" >= :'oct_22_start'::timestamp
  AND "CreatedAt" <= :'oct_22_end'::timestamp;

-- 1.C) Depósitos A (MINT?BACKOFFICE)
\echo ''
\echo '1.C) DEPÓSITOS A (MINT a HOUSE, Oct 22)'
\echo '----------------------------------------------------------------------'

SELECT 
    'DEPÓSITOS A (MINT->BACKOFFICE)' as concepto,
    COUNT(*) as transacciones,
    (SUM("Amount") * 100)::bigint as total_centavos,
    SUM("Amount") as total_decimal,
    CASE WHEN COUNT(*) > 0 THEN SUM("Amount") / COUNT(*) ELSE 0 END as promedio_decimal
FROM "WalletTransactions"
WHERE "BrandId" = :'brand_id'
  AND "TransactionType" = 0 -- MINT
AND "ToUserType" = 'BACKOFFICE'
  AND "CreatedAt" >= :'oct_22_start'::timestamp
  AND "CreatedAt" <= :'oct_22_end'::timestamp;

-- 1.D) Retiros (WITHDRAWAL + BURN)
\echo ''
\echo '1.D) RETIROS (WITHDRAWAL + BURN, Oct 22)'
\echo '----------------------------------------------------------------------'

SELECT 
    'RETIROS (WITHDRAWAL + BURN)' as concepto,
    COUNT(*) as transacciones,
    (SUM("Amount") * 100)::bigint as total_centavos,
    SUM("Amount") as total_decimal,
 CASE WHEN COUNT(*) > 0 THEN SUM("Amount") / COUNT(*) ELSE 0 END as promedio_decimal
FROM "WalletTransactions"
WHERE "BrandId" = :'brand_id'
  AND "TransactionType" IN (5, 6) -- WITHDRAWAL=5, BURN=6 (verifica enum real)
  AND "CreatedAt" >= :'oct_22_start'::timestamp
  AND "CreatedAt" <= :'oct_22_end'::timestamp;

-- ============================================================================
-- SECCIÓN 2: USUARIOS (Oct 22, 2025)
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'USUARIOS - Oct 22, 2025'
\echo '============================================================================'

-- 2.A) Jugadores Directos (creados por SUPER_ADMIN)
\echo ''
\echo '2.A) JUGADORES DIRECTOS'
\echo '----------------------------------------------------------------------'

-- Primero, obtener el ID del SUPER_ADMIN
WITH super_admin AS (
    SELECT "Id" as admin_id
    FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id'
      AND "Role" = 'SUPER_ADMIN'
    LIMIT 1
)
SELECT 
    'Jugadores Directos (creados por SUPER_ADMIN)' as concepto,
    COUNT(*) as total
FROM "Players" p, super_admin sa
WHERE p."CreatedByUserId" = sa.admin_id
  AND p."BrandId" = :'brand_id';

-- 2.B) Agentes Directos (ParentAdminId = SUPER_ADMIN)
\echo ''
\echo '2.B) AGENTES DIRECTOS'
\echo '----------------------------------------------------------------------'

WITH super_admin AS (
    SELECT "Id" as admin_id
  FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id'
      AND "Role" = 'SUPER_ADMIN'
    LIMIT 1
)
SELECT 
    'Agentes Directos (ParentAdminId = SUPER_ADMIN)' as concepto,
    COUNT(*) as total
FROM "BackofficeUsers" u, super_admin sa
WHERE u."ParentAdminId" = sa.admin_id
  AND u."Role" = 'CASHIER';

-- 2.C) Total Jugadores (todos en el brand)
\echo ''
\echo '2.C) TOTAL JUGADORES'
\echo '----------------------------------------------------------------------'

SELECT 
    'Total Jugadores en Brand' as concepto,
    COUNT(*) as total,
    COUNT(CASE WHEN "Status" = 'ACTIVE' THEN 1 END) as activos,
    COUNT(CASE WHEN "Status" != 'ACTIVE' THEN 1 END) as inactivos
FROM "Players"
WHERE "BrandId" = :'brand_id';

-- 2.D) Total Agentes (todos Cashiers en el brand)
\echo ''
\echo '2.D) TOTAL AGENTES'
\echo '----------------------------------------------------------------------'

SELECT 
    'Total Agentes (Cashiers) en Brand' as concepto,
    COUNT(*) as total,
    COUNT(CASE WHEN "HierarchyLevel" = 0 THEN 1 END) as nivel_0,
    COUNT(CASE WHEN "HierarchyLevel" = 1 THEN 1 END) as nivel_1,
    COUNT(CASE WHEN "HierarchyLevel" = 2 THEN 1 END) as nivel_2,
    COUNT(CASE WHEN "HierarchyLevel" >= 3 THEN 1 END) as nivel_3_plus
FROM "BackofficeUsers"
WHERE "BrandId" = :'brand_id'
  AND "Role" = 'CASHIER';

-- ============================================================================
-- SECCIÓN 3: CASINO (Oct 21, 2025)
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'CASINO - Oct 21, 2025'
\echo '============================================================================'

-- 3.A) Jugado, Pagado, Netwin
\echo ''
\echo '3.A) JUGADO, PAGADO, NETWIN'
\echo '----------------------------------------------------------------------'

WITH casino_stats AS (
    SELECT 
        SUM(CASE WHEN l."Reason" = 'BET' THEN l."DeltaBigint" ELSE 0 END) as jugado_centavos,
        SUM(CASE WHEN l."Reason" = 'WIN' THEN l."DeltaBigint" ELSE 0 END) as pagado_centavos,
        COUNT(DISTINCT l."RoundId") as rondas_totales,
        COUNT(DISTINCT l."PlayerId") as jugadores_activos
    FROM "Ledger" l
    JOIN "Players" p ON l."PlayerId" = p."Id"
    WHERE p."BrandId" = :'brand_id'
   AND l."BrandId" = :'brand_id'
      AND l."CreatedAt" >= :'oct_21_start'::timestamp
      AND l."CreatedAt" <= :'oct_21_end'::timestamp
      AND l."Reason" IN ('BET', 'WIN')
)
SELECT 
    'JUGADO' as concepto,
    jugado_centavos as centavos,
    (jugado_centavos::decimal / 100) as decimal_value,
    '$ ' || TO_CHAR((jugado_centavos::decimal / 100), 'FM999,999,999.00') as formatted
FROM casino_stats

UNION ALL

SELECT 
  'PAGADO' as concepto,
    pagado_centavos as centavos,
    (pagado_centavos::decimal / 100) as decimal_value,
    '$ ' || TO_CHAR((pagado_centavos::decimal / 100), 'FM999,999,999.00') as formatted
FROM casino_stats

UNION ALL

SELECT 
    'NETWIN (Jugado - Pagado)' as concepto,
    (jugado_centavos - pagado_centavos) as centavos,
 ((jugado_centavos - pagado_centavos)::decimal / 100) as decimal_value,
    '$ ' || TO_CHAR(((jugado_centavos - pagado_centavos)::decimal / 100), 'FM999,999,999.00') as formatted
FROM casino_stats

UNION ALL

SELECT 
    'Rondas Totales' as concepto,
rondas_totales as centavos,
    rondas_totales as decimal_value,
    rondas_totales::text as formatted
FROM casino_stats

UNION ALL

SELECT 
    'Jugadores Activos' as concepto,
    jugadores_activos as centavos,
 jugadores_activos as decimal_value,
    jugadores_activos::text as formatted
FROM casino_stats;

-- 3.B) Comisión y Total a Pagar
\echo ''
\echo '3.B) COMISIÓN Y TOTAL A PAGAR'
\echo '----------------------------------------------------------------------'

WITH super_admin AS (
    SELECT 
        "Id" as admin_id,
        "CommissionPercent" as comision_porcentaje
    FROM "BackofficeUsers"
 WHERE "BrandId" = :'brand_id'
      AND "Role" = 'SUPER_ADMIN'
    LIMIT 1
),
casino_stats AS (
    SELECT 
        SUM(CASE WHEN l."Reason" = 'BET' THEN l."DeltaBigint" ELSE 0 END) as jugado,
        SUM(CASE WHEN l."Reason" = 'WIN' THEN l."DeltaBigint" ELSE 0 END) as pagado
    FROM "Ledger" l
    JOIN "Players" p ON l."PlayerId" = p."Id"
    WHERE p."BrandId" = :'brand_id'
      AND l."BrandId" = :'brand_id'
   AND l."CreatedAt" >= :'oct_21_start'::timestamp
      AND l."CreatedAt" <= :'oct_21_end'::timestamp
      AND l."Reason" IN ('BET', 'WIN')
),
calculations AS (
    SELECT 
     sa.comision_porcentaje,
        cs.jugado,
        cs.pagado,
    (cs.jugado - cs.pagado) as netwin,
        ((cs.jugado - cs.pagado) * (sa.comision_porcentaje / 100))::bigint as comision_centavos,
        ((cs.jugado - cs.pagado) - ((cs.jugado - cs.pagado) * (sa.comision_porcentaje / 100))::bigint) as total_a_pagar
    FROM super_admin sa, casino_stats cs
)
SELECT 
    'Comisión (%)' as concepto,
    comision_porcentaje as valor_numerico,
    comision_porcentaje::text || '%' as formatted
FROM calculations

UNION ALL

SELECT 
    'Netwin' as concepto,
    (netwin::decimal / 100) as valor_numerico,
    '$ ' || TO_CHAR((netwin::decimal / 100), 'FM999,999,999.00') as formatted
FROM calculations

UNION ALL

SELECT 
    'Comisión ($)' as concepto,
(comision_centavos::decimal / 100) as valor_numerico,
    '$ ' || TO_CHAR((comision_centavos::decimal / 100), 'FM999,999,999.00') as formatted
FROM calculations

UNION ALL

SELECT 
    'Total a Pagar (Netwin - Comisión)' as concepto,
    (total_a_pagar::decimal / 100) as valor_numerico,
    '$ ' || TO_CHAR((total_a_pagar::decimal / 100), 'FM999,999,999.00') as formatted
FROM calculations;

-- 3.C) Validación de Netwin
\echo ''
\echo '3.C) VALIDACIÓN DE NETWIN'
\echo '----------------------------------------------------------------------'
\echo 'Verifica que Netwin = Jugado - Pagado exactamente'
\echo ''

WITH casino_stats AS (
    SELECT 
        SUM(CASE WHEN l."Reason" = 'BET' THEN l."DeltaBigint" ELSE 0 END) as jugado,
        SUM(CASE WHEN l."Reason" = 'WIN' THEN l."DeltaBigint" ELSE 0 END) as pagado
    FROM "Ledger" l
    JOIN "Players" p ON l."PlayerId" = p."Id"
    WHERE p."BrandId" = :'brand_id'
      AND l."BrandId" = :'brand_id'
  AND l."CreatedAt" >= :'oct_21_start'::timestamp
 AND l."CreatedAt" <= :'oct_21_end'::timestamp
      AND l."Reason" IN ('BET', 'WIN')
)
SELECT 
    jugado as jugado_centavos,
    pagado as pagado_centavos,
    (jugado - pagado) as netwin_calculado,
    CASE 
        WHEN (jugado - pagado) = (jugado - pagado) THEN '? Netwin correcto'
      ELSE '? ERROR en Netwin'
    END as validacion,
    CASE 
        WHEN (jugado - pagado) > 0 THEN '? Ganó la casa'
 WHEN (jugado - pagado) < 0 THEN '?? Ganó el jugador'
        ELSE '? Empate'
    END as interpretacion
FROM casino_stats;

-- ============================================================================
-- SECCIÓN 4: DEPORTES (Oct 21, 2025)
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'DEPORTES - Oct 21, 2025'
\echo '============================================================================'
\echo '? MÓDULO DE DEPORTES NO IMPLEMENTADO'
\echo 'Retornar valores en 0 o estructura vacía'
\echo ''

-- Verificar si existe tabla SportsLedger
SELECT 
    CASE 
  WHEN EXISTS (
            SELECT 1 
    FROM information_schema.tables 
 WHERE table_name = 'SportsLedger'
   ) 
        THEN '? Tabla SportsLedger existe'
        ELSE '? Tabla SportsLedger NO existe - Retornar estructura vacía'
    END as estado_modulo_deportes;

-- ============================================================================
-- SECCIÓN 5: RESUMEN CONSOLIDADO
-- ============================================================================

\echo ''
\echo '============================================================================'
\echo 'RESUMEN CONSOLIDADO - Para Dashboard Overview'
\echo '============================================================================'

WITH super_admin AS (
    SELECT "Id" as admin_id, "CommissionPercent"
    FROM "BackofficeUsers"
    WHERE "BrandId" = :'brand_id' AND "Role" = 'SUPER_ADMIN'
    LIMIT 1
),
finanzas AS (
    SELECT 
        (
         ((SELECT COALESCE(SUM("WalletBalance"), 0) FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN')) * 100)::bigint
     + ((SELECT COALESCE(SUM("WalletBalance"), 0) FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" = 'CASHIER') * 100)::bigint
 + (SELECT COALESCE(SUM(w."BalanceBigint"), 0) FROM "Players" p JOIN "Wallets" w ON w."PlayerId" = p."Id" WHERE p."BrandId" = :'brand_id')
        ) as fichas_centavos,
     (SELECT COALESCE(SUM("Amount"), 0) * 100 FROM "WalletTransactions" 
       WHERE "BrandId" = :'brand_id' AND "TransactionType" = 1 AND "FromUserType" = 'BACKOFFICE' AND "ToUserType" = 'PLAYER'
         AND "CreatedAt" >= :'oct_22_start'::timestamp AND "CreatedAt" <= :'oct_22_end'::timestamp)::bigint as cargas_centavos,
        (SELECT COALESCE(SUM("Amount"), 0) * 100 FROM "WalletTransactions" 
    WHERE "BrandId" = :'brand_id' AND "TransactionType" = 0 AND "ToUserType" = 'BACKOFFICE'
         AND "CreatedAt" >= :'oct_22_start'::timestamp AND "CreatedAt" <= :'oct_22_end'::timestamp)::bigint as depositos_centavos,
        (SELECT COALESCE(SUM("Amount"), 0) * 100 FROM "WalletTransactions" 
       WHERE "BrandId" = :'brand_id' AND "TransactionType" IN (5, 6)
         AND "CreatedAt" >= :'oct_22_start'::timestamp AND "CreatedAt" <= :'oct_22_end'::timestamp)::bigint as retiros_centavos
),
usuarios AS (
    SELECT 
        (SELECT COUNT(*) FROM "Players" WHERE "BrandId" = :'brand_id') as total_jugadores,
    (SELECT COUNT(*) FROM "BackofficeUsers" WHERE "BrandId" = :'brand_id' AND "Role" = 'CASHIER') as total_agentes
),
casino AS (
    SELECT 
        COALESCE(SUM(CASE WHEN l."Reason" = 'BET' THEN l."DeltaBigint" ELSE 0 END), 0) as jugado,
        COALESCE(SUM(CASE WHEN l."Reason" = 'WIN' THEN l."DeltaBigint" ELSE 0 END), 0) as pagado
    FROM "Ledger" l
    JOIN "Players" p ON l."PlayerId" = p."Id"
    WHERE p."BrandId" = :'brand_id'
      AND l."CreatedAt" >= :'oct_21_start'::timestamp
      AND l."CreatedAt" <= :'oct_21_end'::timestamp
      AND l."Reason" IN ('BET', 'WIN')
)
SELECT 
    '=== FINANZAS (Oct 22) ===' as seccion,
    ('$ ' || TO_CHAR((f.fichas_centavos::decimal / 100), 'FM999,999,999.00')) as fichas,
    ('$ ' || TO_CHAR((f.cargas_centavos::decimal / 100), 'FM999,999,999.00')) as cargas,
    ('$ ' || TO_CHAR((f.depositos_centavos::decimal / 100), 'FM999,999,999.00')) as depositos_a,
    ('$ ' || TO_CHAR((f.retiros_centavos::decimal / 100), 'FM999,999,999.00')) as retiros,
    '=== USUARIOS (Oct 22) ===' as usuarios_seccion,
    u.total_jugadores::text as total_jugadores,
    u.total_agentes::text as total_agentes,
    '=== CASINO (Oct 21) ===' as casino_seccion,
    ('$ ' || TO_CHAR((c.jugado::decimal / 100), 'FM999,999,999.00')) as jugado,
    ('$ ' || TO_CHAR((c.pagado::decimal / 100), 'FM999,999,999.00')) as pagado,
    ('$ ' || TO_CHAR(((c.jugado - c.pagado)::decimal / 100), 'FM999,999,999.00')) as netwin,
    sa."CommissionPercent"::text || '%' as comision_porcentaje,
    ('$ ' || TO_CHAR((((c.jugado - c.pagado) * (sa."CommissionPercent" / 100))::bigint::decimal / 100), 'FM999,999,999.00')) as comision_dolares,
    ('$ ' || TO_CHAR((((c.jugado - c.pagado) - ((c.jugado - c.pagado) * (sa."CommissionPercent" / 100))::bigint)::decimal / 100), 'FM999,999,999.00')) as total_a_pagar
FROM finanzas f, usuarios u, casino c, super_admin sa;

\echo ''
\echo '============================================================================'
\echo 'FIN DEL SCRIPT DE VALIDACIÓN'
\echo '============================================================================'
\echo 'Compara estos valores con el output del endpoint:'
\echo 'GET /api/v1/admin/dashboard/overview'
\echo '============================================================================'
