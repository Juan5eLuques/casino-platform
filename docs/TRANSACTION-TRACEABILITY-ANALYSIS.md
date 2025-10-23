# ?? Análisis de Trazabilidad de Transacciones para Dashboard

## ?? Problema Actual

**Síntoma**: El dashboard solo muestra **Fichas (Balance)** correctamente, pero devuelve **0** en:
- Cargas
- Depósitos A
- Retiros
- Jugado/Pagado (Casino)
- Comisiones
- Datos de Usuarios

**Usuario afectado**: Admin en `localhost` con:
- 2 Cajeros con balance ($200 y $300)
- 2 Jugadores con balance
- Comisiones configuradas

---

## ?? Análisis del Flujo de Transacciones

### 1. **Sistema de Transacciones Implementado** ?

El sistema tiene **dos fuentes de datos** para transacciones:

#### A) **`WalletTransactions`** (Tabla Principal)
```sql
CREATE TABLE "WalletTransactions" (
    "Id" uuid PRIMARY KEY,
    "BrandId" uuid NOT NULL,
    "FromUserId" uuid NULL,
    "FromUserType" varchar(20) NULL, -- 'BACKOFFICE' o 'PLAYER'
    "ToUserId" uuid NOT NULL,
    "ToUserType" varchar(20) NOT NULL, -- 'BACKOFFICE' o 'PLAYER'
    "Amount" numeric(18,2) NOT NULL, -- En dólares (decimal)
    "TransactionType" integer NULL, -- enum: MINT=0, TRANSFER=1, BET=2, WIN=3, ROLLBACK=4, DEPOSIT=5, WITHDRAWAL=6, BONUS=7, ADJUSTMENT=8
    "PreviousBalanceFrom" numeric(18,2) NULL,
    "NewBalanceFrom" numeric(18,2) NULL,
    "PreviousBalanceTo" numeric(18,2) NULL,
    "NewBalanceTo" numeric(18,2) NULL,
  "Description" varchar(500) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedByRole" varchar(20) NOT NULL,
    "IdempotencyKey" varchar(100) NOT NULL UNIQUE,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Usado por**:
- **Backoffice**: `/api/v1/admin/transactions` (MINT, TRANSFER, DEPOSIT, WITHDRAWAL, BONUS)
- **Gateway/Casino**: `/api/v1/gateway/*` (BET, WIN, ROLLBACK via `UnifiedWalletService`)

#### B) **`Ledger`** (Tabla Secundaria - Solo Casino)
```sql
CREATE TABLE "Ledger" (
    "Id" bigint PRIMARY KEY,
    "BrandId" uuid NOT NULL,
    "PlayerId" uuid NOT NULL,
    "DeltaBigint" bigint NOT NULL, -- En centavos (negativo = débito, positivo = crédito)
    "Reason" varchar(50) NOT NULL, -- enum: BET, WIN, BONUS, ADMIN_GRANT, ADMIN_DEBIT, ROLLBACK, ADJUST, REFUND
    "RoundId" uuid NULL,
    "GameCode" varchar(100) NULL,
    "Provider" varchar(100) NULL,
    "ExternalRef" varchar(255) NULL UNIQUE,
  "Meta" jsonb NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Usado por**:
- **Gateway/Casino**: Registro secundario para compatibilidad
- **Dashboard**: Cálculo de Jugado/Pagado (actualmente)

### 2. **Balances de Usuarios**

#### A) **Players**
```sql
"Players"."WalletBalance" numeric(18,2) DEFAULT 0 -- En dólares (decimal)
```

#### B) **BackofficeUsers**
```sql
"BackofficeUsers"."WalletBalance" numeric(18,2) DEFAULT 0 -- En dólares (decimal)
```

#### C) **Wallets** (Tabla Legacy)
```sql
"Wallets"."BalanceBigint" bigint DEFAULT 0 -- En centavos (legacy, no usado)
```

---

## ?? **Problema Raíz: Inconsistencia en el Sistema**

### Issue #1: **Balance Actual vs Transacciones**

**Problema**: Los balances existen en `BackofficeUsers.WalletBalance` y `Players.WalletBalance`, pero:

1. **NO hay transacciones registradas** en `WalletTransactions` porque:
   - Los balances fueron asignados **manualmente en la BD**
- O fueron creados **antes** de que existiera `WalletTransactions`
   - No hay un sistema de **migración** que genere transacciones históricas

2. **Dashboard busca transacciones** para calcular:
   - Cargas: `TransactionType = TRANSFER AND FromUserType = 'BACKOFFICE' AND ToUserType = 'PLAYER'`
   - Depósitos A: `TransactionType = MINT AND ToUserType = 'BACKOFFICE'`
   - Retiros: `TransactionType IN (WITHDRAWAL, BURN)`

3. **Si no hay transacciones registradas**, el dashboard devuelve **0** aunque existan balances.

### Issue #2: **Jugado/Pagado (Casino)**

**Problema**: Dashboard busca en `Ledger`:

```csharp
// DashboardService.cs línea ~85
var casinoStats = await _db.Ledger
    .Where(l => playerIds.Contains(l.PlayerId)
        && l.BrandId == brandId
        && l.CreatedAt >= from
        && l.CreatedAt <= to
        && (l.Reason == LedgerReason.BET || l.Reason == LedgerReason.WIN))
    .GroupBy(l => 1)
    .Select(g => new
    {
        Jugado = g.Where(l => l.Reason == LedgerReason.BET).Sum(l => l.DeltaBigint),
     Pagado = g.Where(l => l.Reason == LedgerReason.WIN).Sum(l => l.DeltaBigint)
    })
    .FirstOrDefaultAsync(cancellationToken);
```

**Si no hay registros en `Ledger` con `Reason = BET` o `WIN`**, devuelve **0**.

**Esto sucede cuando**:
- No se han hecho apuestas reales vía gateway
- Los balances fueron asignados sin actividad de casino
- El sistema de gateway no se ha usado aún

### Issue #3: **Comisiones**

**Problema**: Dashboard busca comisiones en `CommissionAccruals`:

```csharp
// DashboardService.cs línea ~103
var comisionesAcumuladas = await CalculatePendingCommissionsAsync(userIds, from, to, cancellationToken);

// Si comisionesAcumuladas == 0, estima:
var comisionEstimada = (long)(netwin * (comisionPorcentaje / 100m));
```

**Si Netwin = 0** (porque no hay apuestas), **Comisión = 0**.

### Issue #4: **Usuarios Directos/Totales**

**Problema**: Dashboard busca en `Players.CreatedByUserId`:

```csharp
// DashboardService.cs línea ~131
var jugadoresDirectos = await _db.Players
    .Where(p => p.CreatedByUserId == currentUserId && p.BrandId == brandId)
    .CountAsync(cancellationToken);
```

**Si `Players.CreatedByUserId` es NULL**, no se cuentan.

**Esto sucede cuando**:
- Los jugadores fueron creados sin establecer `CreatedByUserId`
- Migración desde sistema anterior no estableció este campo

---

## ? **Solución: Verificar y Corregir Datos**

### Paso 1: **Verificar Estado de la BD**

Ejecuta este script SQL para diagnosticar:

```sql
-- 1. Verificar balances existentes
SELECT 
    'BACKOFFICE' as tipo,
    "Role",
 COUNT(*) as usuarios,
    SUM("WalletBalance") as balance_total
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
GROUP BY "Role"

UNION ALL

SELECT 
    'PLAYERS' as tipo,
    'PLAYER' as "Role",
    COUNT(*) as usuarios,
    SUM("WalletBalance") as balance_total
FROM "Players"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111';

-- 2. Verificar transacciones registradas
SELECT 
    "TransactionType",
    COUNT(*) as cantidad,
    SUM("Amount") as total
FROM "WalletTransactions"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
GROUP BY "TransactionType";

-- 3. Verificar actividad de casino (Ledger)
SELECT 
    "Reason",
    COUNT(*) as cantidad,
    SUM("DeltaBigint") as total_centavos
FROM "Ledger" l
JOIN "Players" p ON l."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
GROUP BY "Reason";

-- 4. Verificar CreatedByUserId en Players
SELECT 
    CASE WHEN "CreatedByUserId" IS NULL THEN 'NULL' ELSE 'SET' END as created_by_status,
    COUNT(*) as cantidad
FROM "Players"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
GROUP BY CASE WHEN "CreatedByUserId" IS NULL THEN 'NULL' ELSE 'SET' END;

-- 5. Verificar jerarquía de BackofficeUsers
SELECT 
    "Username",
    "Role",
    "ParentAdminId",
    "ParentCashierId",
    "HierarchyLevel",
    "WalletBalance",
    "CreatedByUserId"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY "HierarchyLevel", "Username";
```

### Paso 2: **Soluciones según Diagnóstico**

#### A) **Si hay balances pero NO transacciones**

**Problema**: Balances asignados manualmente sin registro en `WalletTransactions`.

**Solución**: Generar transacciones MINT iniciales:

```sql
-- Insertar MINT inicial para SUPER_ADMIN (si tiene balance)
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
    NULL, -- From: NULL (MINT desde el sistema)
    NULL,
    "Id", -- To: SUPER_ADMIN
    'BACKOFFICE',
    "WalletBalance", -- Monto actual
  0, -- TransactionType.MINT
    NULL,
    NULL,
    0, -- Previous balance 0
    "WalletBalance", -- New balance = balance actual
    'Initial balance - Migration',
    "Id", -- Creado por él mismo
    'SUPER_ADMIN',
    'migration-mint-' || "Id"::text,
    "CreatedAt"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
  AND "Role" IN ('SUPER_ADMIN', 'BRAND_ADMIN')
  AND "WalletBalance" > 0
  AND NOT EXISTS (
      SELECT 1 FROM "WalletTransactions" wt 
      WHERE wt."ToUserId" = "BackofficeUsers"."Id" 
        AND wt."TransactionType" = 0
  );

-- Insertar TRANSFER para Cashiers (desde SUPER_ADMIN)
WITH super_admin AS (
    SELECT "Id", "BrandId" 
    FROM "BackofficeUsers" 
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' 
      AND "Role" = 'SUPER_ADMIN' 
    LIMIT 1
)
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
    1, -- TransactionType.TRANSFER
    0,
    0,
    0,
    bu."WalletBalance",
    'Initial balance - Migration from SUPER_ADMIN',
    sa."Id",
    'SUPER_ADMIN',
    'migration-transfer-cashier-' || bu."Id"::text,
bu."CreatedAt"
FROM "BackofficeUsers" bu
CROSS JOIN super_admin sa
WHERE bu."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND bu."Role" = 'CASHIER'
  AND bu."WalletBalance" > 0
  AND NOT EXISTS (
  SELECT 1 FROM "WalletTransactions" wt 
    WHERE wt."ToUserId" = bu."Id" 
    AND wt."TransactionType" IN (0, 1)
  );

-- Insertar TRANSFER para Players (desde Cashiers)
WITH cashiers AS (
 SELECT "Id", "BrandId" 
    FROM "BackofficeUsers" 
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' 
      AND "Role" = 'CASHIER'
)
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
    c."Id", -- From: CASHIER
    'BACKOFFICE',
 p."Id", -- To: PLAYER
    'PLAYER',
    p."WalletBalance",
    1, -- TransactionType.TRANSFER
    0,
    0,
    0,
    p."WalletBalance",
    'Initial balance - Migration from CASHIER',
    c."Id",
    'CASHIER',
    'migration-transfer-player-' || p."Id"::text,
    p."CreatedAt"
FROM "Players" p
CROSS JOIN cashiers c
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND p."WalletBalance" > 0
  AND NOT EXISTS (
      SELECT 1 FROM "WalletTransactions" wt 
      WHERE wt."ToUserId" = p."Id" 
  AND wt."TransactionType" IN (0, 1)
  )
LIMIT (SELECT COUNT(*) FROM "Players" WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' AND "WalletBalance" > 0);
```

#### B) **Si `Players.CreatedByUserId` es NULL**

**Solución**: Establecer relación con Cashiers:

```sql
-- Opción 1: Asignar todos los players al primer cashier
WITH first_cashier AS (
    SELECT "Id" 
    FROM "BackofficeUsers" 
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' 
      AND "Role" = 'CASHIER' 
    ORDER BY "CreatedAt" 
    LIMIT 1
)
UPDATE "Players" p
SET "CreatedByUserId" = (SELECT "Id" FROM first_cashier)
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
  AND p."CreatedByUserId" IS NULL;

-- Opción 2: Distribuir equitativamente entre cashiers
WITH cashiers AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt") as rn
  FROM "BackofficeUsers"
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
      AND "Role" = 'CASHIER'
),
players_ranked AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt") as rn
    FROM "Players"
    WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
  AND "CreatedByUserId" IS NULL
)
UPDATE "Players" p
SET "CreatedByUserId" = c."Id"
FROM players_ranked pr
JOIN cashiers c ON (pr.rn - 1) % (SELECT COUNT(*) FROM cashiers) + 1 = c.rn
WHERE p."Id" = pr."Id";
```

#### C) **Si NO hay actividad de casino (Ledger vacío)**

**Problema**: No hay apuestas reales, por lo que Jugado/Pagado = 0.

**Solución**:
1. **Crear apuestas de prueba** vía gateway:
   ```bash
   # Crear sesión
   curl -X POST http://localhost:5000/api/v1/internal/sessions
   
   # Hacer apuesta
   curl -X POST http://localhost:5000/api/v1/gateway/bet
   
   # Procesar ganancia
   curl -X POST http://localhost:5000/api/v1/gateway/win
   ```

2. **O insertar datos de prueba en Ledger**:
   ```sql
   -- Insertar apuesta de prueba
   INSERT INTO "Ledger" ("BrandId", "PlayerId", "DeltaBigint", "Reason", "RoundId", "GameCode", "Provider", "ExternalRef", "CreatedAt")
   SELECT 
     p."BrandId",
       p."Id",
 -5000, -- $50 apostados (en centavos negativos)
       'BET',
       gen_random_uuid(),
     'slot-game-01',
       'pragmatic',
       'test-bet-' || p."Id"::text,
       CURRENT_TIMESTAMP
   FROM "Players" p
   WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
   LIMIT 1;
   
   -- Insertar ganancia de prueba
   INSERT INTO "Ledger" ("BrandId", "PlayerId", "DeltaBigint", "Reason", "RoundId", "GameCode", "Provider", "ExternalRef", "CreatedAt")
   SELECT 
       p."BrandId",
       p."Id",
       7500, -- $75 ganados (en centavos positivos)
       'WIN',
       (SELECT "RoundId" FROM "Ledger" WHERE "PlayerId" = p."Id" AND "Reason" = 'BET' ORDER BY "CreatedAt" DESC LIMIT 1),
       'slot-game-01',
       'pragmatic',
       'test-win-' || p."Id"::text,
       CURRENT_TIMESTAMP
   FROM "Players" p
   WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111'
   LIMIT 1;
   ```

---

## ?? **Validación Post-Corrección**

Después de aplicar las correcciones, ejecuta:

```sql
-- Dashboard Overview Query
SELECT 
    '=== FINANZAS ===' as seccion,
    (SELECT COUNT(*) FROM "WalletTransactions" 
     WHERE "TransactionType" = 0 AND "ToUserType" = 'BACKOFFICE') as depositos_mint,
    (SELECT SUM("Amount") FROM "WalletTransactions" 
     WHERE "TransactionType" = 1 AND "FromUserType" = 'BACKOFFICE' AND "ToUserType" = 'PLAYER') as cargas_total,
    (SELECT COUNT(*) FROM "WalletTransactions" 
     WHERE "TransactionType" IN (5, 6)) as retiros_count,
    '=== CASINO ===' as casino_seccion,
(SELECT SUM("DeltaBigint") FROM "Ledger" l JOIN "Players" p ON l."PlayerId" = p."Id" 
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111' AND l."Reason" = 'BET') as jugado_centavos,
  (SELECT SUM("DeltaBigint") FROM "Ledger" l JOIN "Players" p ON l."PlayerId" = p."Id" 
     WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111' AND l."Reason" = 'WIN') as pagado_centavos,
    '=== USUARIOS ===' as usuarios_seccion,
    (SELECT COUNT(*) FROM "Players" 
     WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' AND "CreatedByUserId" IS NOT NULL) as players_con_creator,
    (SELECT COUNT(*) FROM "BackofficeUsers" 
     WHERE "BrandId" = '11111111-1111-1111-1111-111111111111' AND "Role" = 'CASHIER') as cashiers_total;
```

**Valores esperados**:
- `depositos_mint` > 0 (al menos 1 MINT inicial)
- `cargas_total` > 0 (si hay transfers a players)
- `jugado_centavos` != NULL (si hay apuestas)
- `players_con_creator` = total players
- `cashiers_total` = 2 (según tu caso)

---

## ?? **Conclusión**

### **Trazabilidad del Sistema** ?

El sistema **SÍ es trazable** porque:

1. **`WalletTransactions`** registra todas las operaciones financieras:
   - MINT, TRANSFER, DEPOSIT, WITHDRAWAL, BONUS
   - BET, WIN, ROLLBACK (vía gateway)
   - Captura balances antes/después de cada operación
   
2. **`Ledger`** registra actividad de casino:
   - BET, WIN, ROLLBACK
   - Asociado a `RoundId`, `GameCode`, `Provider`

3. **Players** tienen `CreatedByUserId`:
   - Permite queries jerárquicos
   - Soporte para scope TREE

### **Problema Real**: Datos Históricos sin Registros

El dashboard devuelve 0 porque:
- ? Balances asignados manualmente sin transacciones registradas
- ? `Players.CreatedByUserId` NULL ? No se cuentan en jerarquía
- ? Sin actividad de casino ? `Ledger` vacío ? Jugado/Pagado = 0

### **Solución**: Migración de Datos Históricos

Aplica los scripts SQL de **Paso 2** para:
1. Generar transacciones MINT/TRANSFER iniciales
2. Establecer `CreatedByUserId` en Players
3. (Opcional) Insertar datos de prueba en Ledger para casino

Después de esto, el dashboard mostrará datos correctos para **todos los usuarios** en su árbol jerárquico. ?

---

**Archivo creado**: `docs/TRANSACTION-TRACEABILITY-ANALYSIS.md`  
**Fecha**: 2025-01-22
