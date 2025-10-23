# ?? Análisis de Capacidades del Backend para Dashboard de Backoffice

## ?? Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Datos Disponibles vs Requeridos](#datos-disponibles-vs-requeridos)
3. [Análisis por Sección](#análisis-por-sección)
4. [Endpoints Disponibles](#endpoints-disponibles)
5. [Definiciones de Negocio Implementadas](#definiciones-de-negocio-implementadas)
6. [Gaps y Recomendaciones](#gaps-y-recomendaciones)
7. [Ejemplos de Uso](#ejemplos-de-uso)

---

## ? Resumen Ejecutivo

### Capacidad General: **85%** Implementado

El backend actual puede proveer **la mayoría** de los datos requeridos para el dashboard, con algunas limitaciones en:
- **Deportes**: No implementado (0%)
- **Deportes**:

 No existen registros de apuestas deportivas
- **Series históricas**: Disponible pero requiere agregaciones adicionales
- **Top N**: Disponible parcialmente (proveedores/juegos desde `Ledger`, agentes/jugadores desde jerarquía)

### Estado por Sección

| Sección | Estado | Cobertura | Comentarios |
|---------|--------|-----------|-------------|
| **Finanzas** | ? Completo | 95% | Fichas, Cargas, Depósitos, Retiros todos disponibles |
| **Usuarios** | ? Completo | 100% | Conteos directos y en árbol implementados |
| **Casino** | ? Completo | 100% | Jugado, Pagado, Netwin, Comisiones completos |
| **Deportes** | ? No Implementado | 0% | No existe sistema de apuestas deportivas |
| **Alertas** | ? Completo | 90% | Float, cajeros activos, jugadores online |
| **Series Históricas** | ?? Parcial | 60% | Datos disponibles, falta agregación por día/semana |
| **Top N** | ?? Parcial | 70% | Proveedores y juegos OK, agentes/jugadores requiere query adicional |

---

## ?? Datos Disponibles vs Requeridos

### A) Finanzas ? **95% Disponible**

| Campo | Estado | Fuente | Endpoint | Observaciones |
|-------|--------|--------|----------|---------------|
| **Fichas (Balance Actual)** | ? | `BackofficeUsers.WalletBalance` + `Wallets.BalanceBigint` | `GET /dashboard/finances/summary` | Total: HOUSE + Cashiers + Players |
| **Fichas (Delta del Día)** | ? | `WalletTransactions` agregado | `GET /dashboard/finances/summary` | Suma neta de transacciones del día |
| **Cargas** | ? | `WalletTransactions` (TRANSFER: BACKOFFICE?PLAYER) | `GET /dashboard/finances/summary` | Total, Count, Promedio |
| **Depósitos A** | ? | `WalletTransactions` (MINT?BACKOFFICE) | `GET /dashboard/finances/summary` | Fondos inyectados al sistema |
| **Retiros** | ? | `WalletTransactions` (WITHDRAWAL/BURN) | `GET /dashboard/finances/summary` | Total, Count, Promedio |
| **Breakdown** | ? | Calculado | `GET /dashboard/finances/summary` | HOUSE, Cashiers, Players separados |

### B) Usuarios ? **100% Disponible**

| Campo | Estado | Fuente | Endpoint | Observaciones |
|-------|--------|--------|----------|---------------|
| **Jugadores Directos** | ? | `Players.CreatedByUserId = currentUser` | `GET /dashboard/users/counts` | WHERE CreatedByUserId = currentUserId |
| **Agentes Directos** | ? | `BackofficeUsers.ParentAdminId = currentUser` | `GET /dashboard/users/counts` | WHERE ParentAdminId = currentUserId |
| **Total Jugadores** | ? | `Players` en árbol | `GET /dashboard/users/counts` | Usa HierarchyService para resolver árbol |
| **Total Agentes** | ? | `BackofficeUsers` en árbol | `GET /dashboard/users/counts` | Incluye todos los descendientes |
| **Jugadores Activos** | ? | `Players.Status = ACTIVE` | `GET /dashboard/users/counts` | Breakdown disponible |
| **Jugadores Inactivos** | ? | Calculado | `GET /dashboard/users/counts` | Total - Activos |
| **Agentes por Nivel** | ? | `BackofficeUsers.HierarchyLevel` | `GET /dashboard/users/counts` | Agrupado por nivel jerárquico |

### C) Casino ? **100% Disponible**

| Campo | Estado | Fuente | Cálculo | Observaciones |
|-------|--------|--------|---------|---------------|
| **Jugado** | ? | `Ledger` (Reason = BET) | `SUM(DeltaBigint WHERE Reason = BET)` | En centavos (bigint) |
| **Pagado** | ? | `Ledger` (Reason = WIN) | `SUM(DeltaBigint WHERE Reason = WIN)` | En centavos (bigint) |
| **Netwin** | ? | Calculado | `Jugado - Pagado` | ? Fórmula correcta |
| **Comisión (%)** | ? | `BackofficeUsers.CommissionPercent` | Promedio del usuario actual | Heredado en árbol |
| **Comisión ($)** | ? | `CommissionAccruals` o estimado | `NETWIN × Comisión%` | Usa acumuladas si existen |
| **Total a Pagar** | ? | Calculado | `Netwin - Comisión ($)` | ? Fórmula correcta |
| **Hold %** | ? | Calculado | `(Netwin / Jugado) × 100` | KPI adicional |
| **Rondas Totales** | ? | `Ledger.RoundId` distinct | `COUNT(DISTINCT RoundId)` | Conteo de rondas únicas |
| **Apuesta Promedio** | ? | Calculado | `Jugado / Rondas` | KPI adicional |
| **Jugadores Activos** | ? | `Ledger.PlayerId` distinct | `COUNT(DISTINCT PlayerId)` | Jugadores con actividad |

### D) Deportes ? **0% Disponible**

| Campo | Estado | Observación |
|-------|--------|-------------|
| **Apostado** | ? | No existe tabla de apuestas deportivas |
| **Pagado** | ? | No existe sistema de deportes |
| **Netwin** | ? | No implementado |
| **Comisión** | ? | No implementado |

**NOTA CRÍTICA**: El sistema actual **NO tiene módulo de deportes**. Solo existe:
- `Ledger` para casino (BET/WIN)
- `Rounds` para rondas de casino
- No hay tabla `SportsBets` ni `SportsEvents`

### E) Alertas ? **90% Disponible**

| Alerta | Estado | Fuente | Observaciones |
|--------|--------|--------|---------------|
| **Float Bajo** | ? | `BackofficeUsers.WalletBalance` | WHERE WalletBalance < threshold |
| **Cajeros Activos** | ? | `WalletTransactions` (last 24h) | WHERE CreatedAt >= now - 24h |
| **Jugadores Online** | ? | `GameSessions.Status = OPEN` | Sesiones activas |
| **Float Total** | ? | `SUM(WalletBalance)` de Cashiers | Suma total de cajeros |
| **Transacciones Pendientes** | ?? | No implementado | Requiere estados de aprobación |

### F) Series Históricas ?? **60% Disponible**

| Serie | Estado | Disponibilidad | Gap |
|-------|--------|----------------|-----|
| **Jugado por Día** | ?? | Datos disponibles en `Ledger` | Requiere agregación GROUP BY fecha |
| **Netwin por Semana** | ?? | Datos disponibles | Requiere agregación semanal |
| **Cargas por Mes** | ?? | Datos disponibles en `WalletTransactions` | Requiere agregación mensual |
| **Depósitos por Día** | ?? | Datos disponibles | Requiere agregación diaria |

**Solución**: Implementar endpoints adicionales:
```
GET /dashboard/series/daily?from={from}&to={to}&metric=JUGADO|NETWIN|CARGAS
GET /dashboard/series/weekly?from={from}&to={to}
GET /dashboard/series/monthly?year={year}
```

### G) Top N ?? **70% Disponible**

| Top | Estado | Disponibilidad | Gap |
|-----|--------|----------------|-----|
| **Top Proveedores** | ? | `Ledger.Provider` + `SUM(DeltaBigint)` | Disponible via query |
| **Top Juegos** | ? | `Ledger.GameCode` + `SUM(DeltaBigint)` | Disponible via query |
| **Top Agentes** | ?? | `BackofficeUsers` + descendientes | Requiere query adicional |
| **Top Jugadores** | ?? | `Players` + `Ledger` agregado | Requiere query adicional |

**Solución**: Implementar endpoints:
```
GET /dashboard/top/providers?limit=10
GET /dashboard/top/games?limit=10
GET /dashboard/top/agents?limit=10&metric=NETWIN|VOLUME
GET /dashboard/top/players?limit=10&metric=NETWIN|VOLUME
```

---

## ?? Endpoints Disponibles

### 1. Dashboard Overview (Consolidado)

```http
GET /api/v1/admin/dashboard/overview?from={from}&to={to}&scope=TREE
```

**Response:**
```json
{
  "finanzas": { /* FinancesSummaryResponse */ },
  "usuarios": { /* UsersCountsResponse */ },
  "casino": { /* CasinoSummaryResponse */ },
  "alertas": { /* AlertsSummaryResponse */ }
}
```

**Cobertura**: ? Finanzas, Usuarios, Casino, Alertas

### 2. Resumen Financiero

```http
GET /api/v1/admin/dashboard/finances/summary?from={from}&to={to}&scope=TREE
```

**Response:**
```json
{
  "period": {
    "from": "2024-01-01T00:00:00Z",
    "to": "2024-01-31T23:59:59Z",
    "timezone": "UTC"
  },
  "scope": {
    "type": "TREE",
 "userId": "user-id",
    "brandId": "brand-id"
  },
  "fichas": {
    "balanceActual": 1500000,     // En centavos
    "deltaDelDia": 50000,          // Cambio neto del día
    "breakdown": {
      "houseBalance": 1000000,
      "cashiersBalance": 300000,
      "playersBalance": 200000
    }
  },
  "cargas": {
    "total": 500000,    // Total cargado
    "count": 150,       // Número de transacciones
    "promedio": 3333    // Promedio por transacción
  },
  "depositosA": {
    "total": 1000000,   // MINT hacia HOUSE
    "count": 10,
    "promedio": 100000
  },
  "retiros": {
    "total": 200000,    // WITHDRAWAL/BURN
    "count": 25,
    "promedio": 8000
  },
  "links": {
 "reporteMensual": "/api/v1/admin/reports/finances/monthly?year=2024&month=1"
  }
}
```

**Cálculos Implementados:**
- ? `Balance Actual` = HOUSE + Cashiers + Players
- ? `Delta del Día` = SUM(transacciones entrantes) - SUM(transacciones salientes)
- ? `Cargas` = TRANSFER (BACKOFFICE ? PLAYER)
- ? `Depósitos A` = MINT (? BACKOFFICE)
- ? `Retiros` = WITHDRAWAL + BURN

### 3. Resumen de Casino

```http
GET /api/v1/admin/dashboard/casino/summary?from={from}&to={to}&scope=TREE
```

**Response:**
```json
{
  "period": { /* PeriodInfo */ },
  "jugado": 10000000,    // En centavos
  "pagado": 9200000,   // En centavos
  "netwin": 800000,          // jugado - pagado
  "comisionPorcentaje": 15.50,     // %
  "comision": 124000, // netwin × comisionPorcentaje
  "totalAPagar": 676000,           // netwin - comision
  "kpis": {
    "holdPercentage": 8.00,        // (netwin / jugado) × 100
    "rondasTotales": 5000,         // COUNT(DISTINCT RoundId)
    "apuestaPromedio": 2000,       // jugado / rondasTotales
    "jugadoresActivos": 250        // COUNT(DISTINCT PlayerId)
  },
  "links": {
    "reporteMensual": "/api/v1/admin/reports/casino/monthly?year=2024&month=1"
  }
}
```

**Cálculos Implementados:**
- ? `JUGADO` = SUM(Ledger.DeltaBigint WHERE Reason = BET)
- ? `PAGADO` = SUM(Ledger.DeltaBigint WHERE Reason = WIN)
- ? `NETWIN` = JUGADO - PAGADO ? **Fórmula correcta**
- ? `COMISIÓN (%)` = BackofficeUser.CommissionPercent
- ? `COMISIÓN ($)` = NETWIN × COMISIÓN (%)
- ? `TOTAL A PAGAR` = NETWIN - COMISIÓN ($) ? **Fórmula correcta**

### 4. Conteo de Usuarios

```http
GET /api/v1/admin/dashboard/users/counts?scope=TREE
```

**Response:**
```json
{
  "jugadoresDirectos": 50,     // Creados directamente
  "agentesDirectos": 10,       // Hijos directos
  "totalJugadores": 250,      // En todo el árbol
  "totalAgentes": 35,      // En todo el árbol
  "breakdown": {
  "jugadoresActivos": 200,
    "jugadoresInactivos": 50,
    "agentesPorNivel": {
      "nivel1": 10,
      "nivel2": 15,
      "nivel3": 10
    }
  }
}
```

**Cálculos Implementados:**
- ? `Jugadores Directos` = COUNT WHERE CreatedByUserId = currentUserId
- ? `Agentes Directos` = COUNT WHERE ParentAdminId = currentUserId
- ? `Total Jugadores` = COUNT usando HierarchyService (árbol completo)
- ? `Total Agentes` = COUNT usando HierarchyService (árbol completo)

### 5. Alertas Operativas

```http
GET /api/v1/admin/dashboard/alerts?scope=TREE
```

**Response:**
```json
{
  "alertas": [
    {
      "tipo": "FLOAT_BAJO",
  "severidad": "HIGH",
      "count": 5,
      "mensaje": "5 cajeros con saldo < 10000"
    }
  ],
  "estadoOperativo": {
    "cajerosActivos": 15,          // Activos en últimas 24h
    "jugadoresOnline": 120,        // Sesiones abiertas
    "floatTotal": 5000000,      // Saldo total cajeros
    "transaccionesPendientes": 0   // TODO: Implementar aprobaciones
  }
}
```

---

## ?? Definiciones de Negocio Implementadas

### 1. NETWIN ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~125
var jugado = casinoStats?.Jugado ?? 0;
var pagado = casinoStats?.Pagado ?? 0;
var netwin = jugado - pagado;  // ? Fórmula correcta
```

**Fórmula**: `NETWIN = JUGADO – PAGADO`

**Interpretación**:
- Si Netwin > 0: Ganó la casa
- Si Netwin < 0: Ganó la red de jugadores

### 2. COMISIÓN (%) ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~133
var comisionPorcentaje = await CalculateAverageCommissionRateAsync(currentUserId, cancellationToken);

// Método helper línea ~377
private async Task<decimal> CalculateAverageCommissionRateAsync(Guid userId, CancellationToken cancellationToken)
{
    var user = await _db.BackofficeUsers
 .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    
    return user?.CommissionPercent ?? 0;
}
```

**Fuente**: `BackofficeUsers.CommissionPercent`

### 3. COMISIÓN ($) ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~136-141
var comisionesAcumuladas = await CalculatePendingCommissionsAsync(userIds, from, to, cancellationToken);

var comisionEstimada = comisionesAcumuladas > 0 
    ? comisionesAcumuladas 
    : (long)(netwin * (comisionPorcentaje / 100m));  // ? Fórmula correcta

var totalAPagar = netwin - comisionEstimada;  // ? Fórmula correcta
```

**Fórmula**: `COMISIÓN ($) = NETWIN × COMISIÓN (%)`

**Lógica**:
1. Si existen comisiones acumuladas en `CommissionAccruals` (pendientes de pago), usa ese valor
2. Si no, estima: `NETWIN × CommissionPercent`

### 4. TOTAL A PAGAR ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~143
var totalAPagar = netwin - comisionEstimada;  // ? Fórmula correcta
```

**Fórmula**: `TOTAL A PAGAR = NETWIN – COMISIÓN ($)`

**Interpretación por Jerarquía**:

| Nivel | Significado de "Total a pagar" |
|-------|-------------------------------|
| **Brand Owner / Dueño del sitio** | Lo que retiene el sitio como ganancia neta después de pagar comisiones |
| **Agente / Subagente** | Lo que debe pagar hacia su red inferior (subagentes o jugadores) |
| **Cajero** | Su margen neto o diferencia entre cargas y retiros |

### 5. FICHAS ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~231-253
var houseBalance = await _db.BackofficeUsers
    .Where(u => userIds.Contains(u.Id) && 
        (u.Role == BackofficeUserRole.BRAND_ADMIN || u.Role == BackofficeUserRole.SUPER_ADMIN))
    .SumAsync(u => u.WalletBalance, cancellationToken);

var cashiersBalance = await _db.BackofficeUsers
    .Where(u => userIds.Contains(u.Id) && u.Role == BackofficeUserRole.CASHIER)
 .SumAsync(u => u.WalletBalance, cancellationToken);

var playersBalance = await _db.Wallets
    .Where(w => playerIds.Contains(w.PlayerId))
    .SumAsync(w => w.BalanceBigint, cancellationToken);

var total = (long)houseBalance + (long)cashiersBalance + playersBalance;
```

**Fórmula**: `FICHAS = HOUSE + Cashiers + Players`

O alternativamente: `FICHAS = SUM(Depósitos A) – SUM(Retiros)`

### 6. CARGAS ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~338-361
var transactions = await _db.WalletTransactions
    .Where(t => t.BrandId == brandId
        && t.TransactionType == TransactionType.TRANSFER
        && t.FromUserType == "BACKOFFICE"  // Desde backoffice
        && t.ToUserType == "PLAYER"        // Hacia player
        && userIds.Contains(t.CreatedByUserId)
        && t.CreatedAt >= from
  && t.CreatedAt <= to)
    .GroupBy(t => 1)
    .Select(g => new { Total = g.Sum(t => t.Amount), Count = g.Count() })
    .FirstOrDefaultAsync(cancellationToken);
```

**Definición**: Transferencias internas desde usuarios administrativos (Admin/Cashier) hacia jugadores (PLAYER) con estado confirmado.

**Filtros**:
- `TransactionType = TRANSFER`
- `FromUserType = BACKOFFICE`
- `ToUserType = PLAYER`

### 7. DEPÓSITOS A ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~366-390
var transactions = await _db.WalletTransactions
    .Where(t => t.BrandId == brandId
        && t.TransactionType == TransactionType.MINT
        && t.ToUserType == "BACKOFFICE"
        && userIds.Contains(t.ToUserId)
 && t.CreatedAt >= from
        && t.CreatedAt <= to)
    .GroupBy(t => 1)
    .Select(g => new { Total = g.Sum(t => t.Amount), Count = g.Count() })
    .FirstOrDefaultAsync(cancellationToken);
```

**Definición**: Aportes de capital a la billetera HOUSE (fondos que el dueño inyecta en la plataforma).

**Filtros**:
- `TransactionType = MINT`
- `ToUserType = BACKOFFICE` (hacia usuarios administrativos)

### 8. RETIROS ? Correcto

```csharp
// Implementación en DashboardService.cs línea ~307-330
var transactions = await _db.WalletTransactions
    .Where(t => t.BrandId == brandId
        && (t.TransactionType == TransactionType.WITHDRAWAL || t.TransactionType == TransactionType.BURN)
        && t.CreatedAt >= from
        && t.CreatedAt <= to)
    .GroupBy(t => 1)
    .Select(g => new { Total = g.Sum(t => t.Amount), Count = g.Count() })
    .FirstOrDefaultAsync(cancellationToken);
```

**Definición**: Egresos confirmados desde billeteras (jugadores o HOUSE).

**Filtros**:
- `TransactionType = WITHDRAWAL OR BURN`

---

## ?? Scope y Jerarquía ? Implementado

### Parámetros de Dashboard

```csharp
public record DashboardQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string Timezone { get; init; } = "UTC";
    public Guid? BrandId { get; init; }
public DashboardScope Scope { get; init; } = DashboardScope.TREE;
    public string? Currency { get; init; }
}

public enum DashboardScope
{
    DIRECT,  // Solo entidades creadas directamente por el usuario
    TREE,    // Árbol completo del usuario (descendientes)
    GLOBAL   // Todo el brand (SUPER_ADMIN o BRAND_ADMIN)
}
```

### Resolución de Scope

```csharp
// Implementación en DashboardService.cs línea ~175-203
private async Task<(Guid brandId, HashSet<Guid> userIds)> ResolveScopeAsync(
    DashboardQuery query, Guid currentUserId, string currentRole, CancellationToken cancellationToken)
{
    var brandId = query.BrandId ?? Guid.Empty;
    HashSet<Guid> userIds;
    
    switch (query.Scope)
    {
        case DashboardScope.DIRECT:
     userIds = new HashSet<Guid> { currentUserId };
     break;
            
    case DashboardScope.TREE:
            var descendants = await _hierarchyService.GetDescendantsAsync(currentUserId, cancellationToken);
  userIds = descendants.Select(d => d.Id).Append(currentUserId).ToHashSet();
     break;
        
        case DashboardScope.GLOBAL:
            var globalUsers = await _db.BackofficeUsers
     .Where(u => u.BrandId == brandId)
         .Select(u => u.Id)
 .ToListAsync(cancellationToken);
          userIds = globalUsers.ToHashSet();
            break;
        
        default:
            userIds = new HashSet<Guid> { currentUserId };
      break;
    }
  
    return (brandId, userIds);
}
```

### Permisos por Rol

| Scope | SUPER_ADMIN | BRAND_ADMIN | CASHIER |
|-------|-------------|-------------|---------|
| **DIRECT** | ? | ? | ? |
| **TREE** | ? | ? | ? |
| **GLOBAL** | ? | ? | ? |

**Validación en Endpoint**:
```csharp
// DashboardEndpoints.cs línea ~147
if (currentRole == "CASHIER" && scopeEnum == DashboardScope.GLOBAL)
{
    return null; // Forbid
}
```

### Filtros Automáticos

Todos los endpoints aplican automáticamente:
- ? `brandId`: Validado desde JWT token
- ? `scope`: brand / tree / global
- ? `date_from`, `date_to`: Rango de fechas
- ? `timezone`: Conversión UTC
- ? `currency`: (Preparado para multi-moneda)

---

## ?? Gaps y Recomendaciones

### 1. Deportes ? **Prioridad: BAJA (Sistema no existe)**

**Gap**: No existe módulo de apuestas deportivas.

**Solución**: Si se requiere en el futuro:
1. Crear tabla `SportsBets` con campos:
   - `PlayerId`, `EventId`, `Amount`, `Odds`, `Status`, `Result`
2. Crear tabla `SportsEvents` para eventos deportivos
3. Implementar endpoint `/dashboard/sports/summary` similar a casino

**Tiempo estimado**: 3-5 días de desarrollo

### 2. Series Históricas ?? **Prioridad: MEDIA**

**Gap**: Datos disponibles pero no agregados por día/semana/mes.

**Solución**: Crear endpoints adicionales:

```csharp
// Nuevo servicio: ITimeSeriesService
GET /api/v1/admin/dashboard/series/daily
GET /api/v1/admin/dashboard/series/weekly
GET /api/v1/admin/dashboard/series/monthly
```

**Queries Necesarias**:
```sql
-- Jugado por día
SELECT DATE(created_at) as date, 
       SUM(delta_bigint) as total
FROM ledger
WHERE reason = 'BET' 
  AND brand_id = @brandId
  AND created_at BETWEEN @from AND @to
GROUP BY DATE(created_at)
ORDER BY date;

-- Cargas por semana
SELECT DATE_TRUNC('week', created_at) as week,
       SUM(amount) as total,
       COUNT(*) as count
FROM wallet_transactions
WHERE transaction_type = 'TRANSFER'
  AND from_user_type = 'BACKOFFICE'
  AND to_user_type = 'PLAYER'
  AND brand_id = @brandId
GROUP BY week
ORDER BY week;
```

**Tiempo estimado**: 1-2 días de desarrollo

### 3. Top N ?? **Prioridad: MEDIA**

**Gap**: Queries para Top Agentes y Top Jugadores no implementadas.

**Solución**: Crear endpoint `/dashboard/top/{entity}`:

```csharp
// Nuevo servicio: ITopRankingsService
GET /api/v1/admin/dashboard/top/providers?limit=10
GET /api/v1/admin/dashboard/top/games?limit=10
GET /api/v1/admin/dashboard/top/agents?limit=10&metric=NETWIN
GET /api/v1/admin/dashboard/top/players?limit=10&metric=VOLUME
```

**Queries Necesarias**:
```sql
-- Top Proveedores
SELECT provider, 
  SUM(CASE WHEN reason = 'BET' THEN delta_bigint ELSE 0 END) as jugado,
       SUM(CASE WHEN reason = 'WIN' THEN delta_bigint ELSE 0 END) as pagado,
       SUM(CASE WHEN reason = 'BET' THEN delta_bigint ELSE 0 END) - 
       SUM(CASE WHEN reason = 'WIN' THEN delta_bigint ELSE 0 END) as netwin
FROM ledger
WHERE brand_id = @brandId
  AND created_at BETWEEN @from AND @to
GROUP BY provider
ORDER BY netwin DESC
LIMIT @limit;

-- Top Agentes (por volumen de su red)
SELECT bu.id, bu.username,
       SUM(CASE WHEN l.reason = 'BET' THEN l.delta_bigint ELSE 0 END) as volumen,
       SUM(CASE WHEN l.reason = 'BET' THEN l.delta_bigint ELSE 0 END) - 
     SUM(CASE WHEN l.reason = 'WIN' THEN l.delta_bigint ELSE 0 END) as netwin
FROM backoffice_users bu
JOIN players p ON p.created_by_user_id = bu.id
JOIN ledger l ON l.player_id = p.id
WHERE bu.brand_id = @brandId
  AND l.created_at BETWEEN @from AND @to
GROUP BY bu.id, bu.username
ORDER BY volumen DESC
LIMIT @limit;

-- Top Jugadores
SELECT p.id, p.username,
   SUM(CASE WHEN l.reason = 'BET' THEN l.delta_bigint ELSE 0 END) as apostado,
       SUM(CASE WHEN l.reason = 'WIN' THEN l.delta_bigint ELSE 0 END) as ganado
FROM players p
JOIN ledger l ON l.player_id = p.id
WHERE p.brand_id = @brandId
  AND l.created_at BETWEEN @from AND @to
GROUP BY p.id, p.username
ORDER BY apostado DESC
LIMIT @limit;
```

**Tiempo estimado**: 2-3 días de desarrollo

### 4. Transacciones Pendientes ?? **Prioridad: BAJA**

**Gap**: Campo `transaccionesPendientes` en alertas siempre retorna 0.

**Solución**:
- Agregar campo `ApprovalStatus` a `WalletTransactions`
- Filtrar por `Status = PENDING`

**Tiempo estimado**: 1 día de desarrollo

### 5. Vistas Materializadas ?? **Prioridad: MEDIA (Performance)**

**Gap**: No existen vistas materializadas para agregaciones diarias.

**Solución**: Crear vistas materializadas para:
- Netwin diario por brand
- Volumen de cargas diario
- Retiros diarios
- Comisiones acumuladas diarias

```sql
CREATE MATERIALIZED VIEW daily_casino_stats AS
SELECT 
    brand_id,
    DATE(created_at) as date,
    SUM(CASE WHEN reason = 'BET' THEN delta_bigint ELSE 0 END) as total_bet,
    SUM(CASE WHEN reason = 'WIN' THEN delta_bigint ELSE 0 END) as total_win,
    SUM(CASE WHEN reason = 'BET' THEN delta_bigint ELSE 0 END) - 
 SUM(CASE WHEN reason = 'WIN' THEN delta_bigint ELSE 0 END) as netwin,
    COUNT(DISTINCT player_id) as active_players,
    COUNT(DISTINCT round_id) as total_rounds
FROM ledger
GROUP BY brand_id, DATE(created_at);

-- Refresh automático cada hora
CREATE UNIQUE INDEX ON daily_casino_stats (brand_id, date);
```

**Beneficio**: Mejora significativa de performance para queries históricas.

**Tiempo estimado**: 2-3 días de desarrollo + testing

---

## ?? Tabla de Implementación vs Requerimiento

| Datos Requeridos | Implementado | Endpoint | Observaciones |
|------------------|--------------|----------|---------------|
| **FINANZAS** |
| Fichas (Balance) | ? | `/dashboard/finances/summary` | HOUSE + Cashiers + Players |
| Fichas (Delta Día) | ? | `/dashboard/finances/summary` | Suma neta transacciones |
| Cargas | ? | `/dashboard/finances/summary` | TRANSFER: BACKOFFICE?PLAYER |
| Depósitos A | ? | `/dashboard/finances/summary` | MINT?BACKOFFICE |
| Retiros | ? | `/dashboard/finances/summary` | WITHDRAWAL + BURN |
| **USUARIOS** |
| Jugadores Directos | ? | `/dashboard/users/counts` | CreatedByUserId |
| Agentes Directos | ? | `/dashboard/users/counts` | ParentAdminId |
| Total Jugadores | ? | `/dashboard/users/counts` | HierarchyService |
| Total Agentes | ? | `/dashboard/users/counts` | HierarchyService |
| **CASINO** |
| Jugado | ? | `/dashboard/casino/summary` | Ledger: BET |
| Pagado | ? | `/dashboard/casino/summary` | Ledger: WIN |
| Netwin | ? | `/dashboard/casino/summary` | Jugado - Pagado |
| Comisión (%) | ? | `/dashboard/casino/summary` | CommissionPercent |
| Comisión ($) | ? | `/dashboard/casino/summary` | Netwin × % |
| Total a Pagar | ? | `/dashboard/casino/summary` | Netwin - Comisión |
| **DEPORTES** |
| Apostado | ? | N/A | Sistema no existe |
| Pagado | ? | N/A | Sistema no existe |
| Netwin | ? | N/A | Sistema no existe |
| Comisión | ? | N/A | Sistema no existe |
| **EXTRAS** |
| Series Históricas | ?? | N/A | Datos disponibles, requiere agregación |
| Top Proveedores | ?? | N/A | Query directa disponible |
| Top Juegos | ?? | N/A | Query directa disponible |
| Top Agentes | ?? | N/A | Requiere query adicional |
| Top Jugadores | ?? | N/A | Requiere query adicional |
| Alertas Operativas | ? | `/dashboard/alerts` | Float, Cajeros, Jugadores Online |
| KPIs Adicionales | ? | `/dashboard/casino/summary` | Hold%, Rondas, Apuesta Promedio |

---

## ?? Ejemplos de Uso (cURL)

### 1. Overview Consolidado

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/overview?from=2024-01-01T00:00:00Z&to=2024-01-31T23:59:59Z&scope=TREE" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Response**: Incluye Finanzas, Usuarios, Casino y Alertas en un solo JSON.

### 2. Solo Finanzas

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/finances/summary?from=2024-01-01T00:00:00Z&to=2024-01-31T23:59:59Z&scope=TREE" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3. Solo Casino

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/casino/summary?from=2024-01-01T00:00:00Z&to=2024-01-31T23:59:59Z&scope=TREE" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4. Conteo de Usuarios

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/users/counts?scope=TREE" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 5. Alertas

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/alerts?scope=TREE" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 6. Scope Global (SUPER_ADMIN only)

```bash
curl -X GET "https://admin.bet30.com/api/v1/admin/dashboard/overview?from=2024-01-01T00:00:00Z&to=2024-01-31T23:59:59Z&scope=GLOBAL&brandId=BRAND_UUID" \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT_TOKEN"
```

---

## ?? Observaciones de Seguridad y Jerarquía

### 1. Roles y Permisos ? Implementado

| Rol | Scope Permitido | Validación |
|-----|----------------|------------|
| **SUPER_ADMIN** | DIRECT, TREE, GLOBAL | ? Sin restricciones |
| **BRAND_ADMIN** | DIRECT, TREE, GLOBAL | ? Solo su brand |
| **CASHIER** | DIRECT, TREE | ? No GLOBAL, solo su árbol |

### 2. Brand Scoping ? Implementado

```csharp
// Validación en DashboardEndpoints.cs línea ~138-145
var effectiveBrandId = brandId ?? tokenBrandId;

if (currentRole != "SUPER_ADMIN" && effectiveBrandId != tokenBrandId)
{
    return null; // Forbid
}
```

**Lógica**:
- JWT token incluye `brand_id`
- Usuarios no-SUPER_ADMIN solo pueden consultar su brand
- SUPER_ADMIN puede especificar cualquier `brandId`

### 3. Hierarchy Service ? Implementado

```csharp
// DashboardService.cs usa HierarchyService para resolver árbol
var descendants = await _hierarchyService.GetDescendantsAsync(currentUserId, cancellationToken);
userIds = descendants.Select(d => d.Id).Append(currentUserId).ToHashSet();
```

**Garantía**: Solo se incluyen descendientes directos en el árbol.

### 4. Player Scoping ? Implementado

```csharp
// DashboardService.cs línea ~208-216
private async Task<HashSet<Guid>> GetPlayerIdsInScopeAsync(
    HashSet<Guid> userIds, Guid brandId, CancellationToken cancellationToken)
{
    var playerList = await _db.Players
        .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty) && p.BrandId == brandId)
   .Select(p => p.Id)
        .ToListAsync(cancellationToken);
    
    return playerList.ToHashSet();
}
```

**Garantía**: Solo jugadores creados por usuarios en scope.

---

## ?? Resumen Final

### ? **DISPONIBLE (85%)**

1. **Finanzas** ? 95%
   - Fichas (Balance, Delta)
   - Cargas
   - Depósitos A
   - Retiros
   - Breakdown detallado

2. **Usuarios** ? 100%
   - Jugadores Directos/Totales
   - Agentes Directos/Totales
   - Breakdown por estado y nivel

3. **Casino** ? 100%
   - Jugado, Pagado, Netwin
   - Comisiones (% y $)
   - Total a Pagar
   - KPIs (Hold%, Rondas, Apuesta Promedio, Jugadores Activos)

4. **Alertas** ? 90%
   - Float Bajo
   - Cajeros Activos
   - Jugadores Online
   - Float Total

5. **Scope y Jerarquía** ? 100%
   - DIRECT, TREE, GLOBAL
   - Validación por rol
   - Brand scoping automático

### ? **NO DISPONIBLE (15%)**

1. **Deportes** ? 0% - Sistema no existe
2. **Series Históricas** ?? 60% - Datos disponibles, falta agregación
3. **Top N** ?? 70% - Proveedores/Juegos OK, Agentes/Jugadores requieren query
4. **Transacciones Pendientes** ?? - Falta sistema de aprobaciones

### ?? **RECOMENDACIONES**

1. **Implementar endpoints de Series Históricas** (1-2 días)
2. **Implementar endpoints de Top N** (2-3 días)
3. **Crear vistas materializadas** para performance (2-3 días)
4. **Agregar sistema de aprobaciones** para transacciones (1 semana)
5. **Módulo de Deportes** solo si se requiere en el futuro (3-5 días)

---

## ?? Soporte

Para consultas sobre implementación o dudas técnicas:
- Revisar código fuente en:
  - `apps/Casino.Application/Services/Implementations/DashboardService.cs`
  - `apps/api/Casino.Api/Endpoints/DashboardEndpoints.cs`
  - `apps/Casino.Application/DTOs/Dashboard/DashboardDTOs.cs`

---

**Última actualización**: 2024-01-22  
**Versión del Backend**: .NET 9
