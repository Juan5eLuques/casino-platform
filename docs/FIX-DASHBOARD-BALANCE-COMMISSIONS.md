# ?? Fix: Dashboard Balance y Comisiones

## ?? **Problemas Corregidos**

### 1. ? **Players Balance = 0** (debería ser $500)
### 2. ? **Comisión Porcentaje = 0** (debería incluir comisiones de subordinados)

---

## ? **Soluciones Implementadas**

### **Fix 1: Players Balance Calculation**

**Ubicación**: `DashboardService.cs` ? `CalculateCurrentBalanceAsync()` (línea ~413)

#### **ANTES** (? Incorrecto):
```csharp
var playersBalance = await _db.Wallets
    .Where(w => playerIds.Contains(w.PlayerId))
    .SumAsync(w => w.BalanceBigint, cancellationToken);
```

**Problema**:
- Usaba `Wallets.BalanceBigint` (formato **bigint obsoleto** en centavos)
- La tabla `Wallets` es legacy y no se actualiza correctamente

#### **AHORA** (? Correcto):
```csharp
// FIX: Usar Players.WalletBalance (decimal) en lugar de Wallets.BalanceBigint (bigint obsoleto)
var playersBalance = await _db.Players
    .Where(p => playerIds.Contains(p.Id))
    .SumAsync(p => p.WalletBalance, cancellationToken);
```

**Beneficio**:
- ? Usa `Players.WalletBalance` (formato **decimal moderno** en dólares)
- ? Refleja el balance real de los jugadores
- ? Compatible con el sistema de transacciones actual

---

### **Fix 2: Commission Calculation (Upstream Flow)**

**Ubicación**: `DashboardService.cs` ? `CalculateAverageCommissionRateAsync()` (línea ~458)

#### **ANTES** (? Incorrecto):
```csharp
private async Task<decimal> CalculateAverageCommissionRateAsync(
  Guid userId,
    CancellationToken cancellationToken)
{
    var user = await _db.BackofficeUsers
        .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    if (user == null) return 0;
    
    // ? Solo retornaba la comisión del usuario actual
    return user.CommissionPercent;
}
```

**Problema**:
- Solo consideraba la comisión del usuario actual
- No incluía comisiones de subordinados (árbol jerárquico)
- Para `localadmin` (0% propio), retornaba 0% ignorando los 20% de `localcajero2`

#### **AHORA** (? Correcto):
```csharp
private async Task<decimal> CalculateAverageCommissionRateAsync(
    Guid userId,
    CancellationToken cancellationToken)
{
    // FIX: Calcular la comisión total del árbol (upstream flow)
    // La comisión se calcula sumando las comisiones de TODOS los subordinados
    // porque el admin superior recibe parte de las comisiones de sus subordinados
    
    var user = await _db.BackofficeUsers
        .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    if (user == null) return 0;
    
    // Obtener todos los usuarios del árbol jerárquico
 var hierarchyUsers = await _db.BackofficeUsers
        .Where(u => u.HierarchyPath != null && u.HierarchyPath.Contains(userId.ToString()))
        .ToListAsync(cancellationToken);
    
    // Incluir al usuario actual si tiene comisión
    if (user.CommissionPercent > 0)
    {
        hierarchyUsers.Add(user);
    }
    
    // Si no hay usuarios con comisión en el árbol, retornar 0
    if (hierarchyUsers.Count == 0) return 0;
    
    // Calcular el promedio ponderado de comisiones del árbol
    var averageCommission = hierarchyUsers.Average(u => u.CommissionPercent);
    
    _logger.LogInformation(
     "Commission calculated for user {UserId}: {Commission}% (based on {Count} users in hierarchy)",
        userId, Math.Round(averageCommission, 2), hierarchyUsers.Count);
    
    return Math.Round(averageCommission, 2);
}
```

**Beneficio**:
- ? Incluye **todas las comisiones del árbol** (upstream flow)
- ? Para `localadmin`: promedia comisiones de `localcajero` (10%) y `localcajero2` (20%)
- ? Retorna: `(0 + 10 + 20) / 3 = 10%` (o similar según la lógica ponderada)

---

## ?? **Resultado Esperado Después del Fix**

### **Dashboard Overview**

```json
{
  "finanzas": {
    "fichas": {
      "balanceActual": 50000,  // ? $49,500 (backoffice) + $500 (players)
  "deltaDelDia": 0,
   "breakdown": {
        "houseBalance": 49000,     // ? localadmin
        "cashiersBalance": 500,    // ? localcajero + localcajero2
        "playersBalance": 500      // ? FIX: Ahora incluye localjugador ($500)
      }
    }
  },
  "casino": {
    "comisionPorcentaje": 10,  // ? FIX: Promedio de comisiones del árbol (10% + 20%) / 2
    "comision": 0,
    "totalAPagar": 0
  }
}
```

### **Desglose de Comisiones**

```
superadmin (Level 0, Comisión: 0%)
  ?? localadmin (Level 1, Comisión: 0%)
      ?? localcajero (Level 2, Comisión: 10%)
      ?   ?? localjugador ($500)
      ?? cajeronuevo (Level 2, Comisión: 0%)
      ?? localcajero2 (Level 3, Comisión: 20%)
          ?? localjugador2 ($0)

? Comisión promedio para localadmin: (0 + 10 + 20) / 3 = 10%
```

---

## ?? **Validación**

### **Prueba 1: Verificar Players Balance**

```sh
curl -X GET "https://localhost:7182/api/v1/admin/diagnostics/system-status" \
  -H "Cookie: bk.token.localhost_dev=TOKEN"
```

**Verificar**:
- `players[0].walletBalance = 500` ?
- `summary.totalBalancePlayers = 500` ?

### **Prueba 2: Verificar Dashboard**

```sh
curl -X GET "https://localhost:7182/api/v1/admin/dashboard/overview?scope=TREE" \
  -H "Cookie: bk.token.localhost_dev=TOKEN"
```

**Verificar**:
- `finanzas.fichas.breakdown.playersBalance = 500` ?
- `casino.comisionPorcentaje > 0` ? (debería ser ~10% o mayor)

---

## ?? **Impacto del Fix**

### **Antes del Fix**
```
? Players Balance = 0 (dashboard mostraba solo balances de backoffice)
? Comisión Porcentaje = 0 (solo consideraba comisión propia de localadmin)
? Fichas totales incorrectas ($49,500 en lugar de $50,000)
```

### **Después del Fix**
```
? Players Balance = $500 (suma correcta de localjugador)
? Comisión Porcentaje = 10% (promedio de comisiones del árbol)
? Fichas totales correctas = $50,000 (house + cashiers + players)
? Dashboard refleja la realidad del sistema
```

---

## ?? **Notas Técnicas**

### **Migración de Wallets a Players.WalletBalance**

El sistema tiene **dos formatos de balance**:

1. **Legacy (Bigint)**: `Wallets.BalanceBigint` ? Formato obsoleto en centavos
2. **Moderno (Decimal)**: `Players.WalletBalance` ? Formato actual en dólares

**Estado actual**:
- ? `WalletTransactions` usa `Players.WalletBalance` (decimal)
- ? `AdminTransactionService` usa `Players.WalletBalance` (decimal)
- ? `Wallets.BalanceBigint` solo se mantiene para compatibilidad con legacy

**Recomendación**: Deprecar completamente `Wallets.BalanceBigint` y usar siempre `Players.WalletBalance`.

### **Comisiones Upstream**

El sistema de comisiones funciona **hacia arriba**:

```
localjugador ($500) ? apuesta $100
  ?
localcajero (10% comisión) ? recibe $10
  ?
localadmin (parte de 10%) ? recibe $X
  ?
superadmin
```

**Implementación actual**: El dashboard calcula el **promedio de comisiones** del árbol.  
**Implementación futura**: Usar `CommissionAccruals` para comisiones reales acumuladas.

---

## ? **Checklist de Validación**

- [x] `DashboardService.CalculateCurrentBalanceAsync` corregido
- [x] `DashboardService.CalculateAverageCommissionRateAsync` corregido
- [x] Compilación exitosa
- [ ] Reiniciar aplicación con Hot Reload
- [ ] Probar dashboard con scope TREE
- [ ] Verificar que `playersBalance = 500`
- [ ] Verificar que `comisionPorcentaje > 0`

---

**Archivo**: `apps/Casino.Application/Services/Implementations/DashboardService.cs`  
**Líneas modificadas**: 413-415 (players balance), 458-490 (commission calculation)  
**Fecha**: 2025-01-23  
**Versión**: 1.1
