# ?? Solución: Dashboard mostrando datos vacíos

## ?? Problema Identificado

El dashboard muestra todos los valores en 0 o vacíos porque:

1. **Scope TREE** para SUPER_ADMIN sin descendientes jerárquicos devuelve solo el usuario actual
2. **GetPlayerIdsInScopeAsync** filtra players por `CreatedByUserId`, excluyendo players creados fuera del árbol
3. **Cashiers y Players** no están siendo incluidos en el scope correcto

## ? Soluciones Implementadas

### 1. **Fallback Automático para SUPER_ADMIN sin descendientes**

```csharp
// En ResolveScopeAsync() - línea ~186
case DashboardScope.TREE:
    var descendants = await _hierarchyService.GetDescendantsAsync(currentUserId, cancellationToken);
    userIds = descendants.Select(d => d.Id).Append(currentUserId).ToHashSet();
    
    // FIX: Si SUPER_ADMIN no tiene descendientes, usar GLOBAL
    if (userIds.Count == 1 && currentRole == "SUPER_ADMIN")
    {
        var allBrandUsers = await _db.BackofficeUsers
       .Where(u => u.BrandId == brandId)
            .Select(u => u.Id)
    .ToListAsync(cancellationToken);
        userIds = allBrandUsers.ToHashSet();
    }
    break;
```

**Resultado**: SUPER_ADMIN siempre ve TODOS los usuarios del brand en scope TREE.

### 2. **Mejora en GetPlayerIdsInScopeAsync para GLOBAL**

```csharp
// En GetPlayerIdsInScopeAsync() - línea ~208
if (userIds.Count > 50) // Threshold: probablemente es GLOBAL
{
    // Incluir TODOS los players del brand
    var allPlayers = await _db.Players
      .Where(p => p.BrandId == brandId)
        .Select(p => p.Id)
        .ToListAsync(cancellationToken);
    
    playerIds = allPlayers.ToHashSet();
}
else
{
    // Filtrar por CreatedByUserId solo para TREE/DIRECT
    var playerList = await _db.Players
        .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty) && p.BrandId == brandId)
        .Select(p => p.Id)
        .ToListAsync(cancellationToken);
    
    playerIds = playerList.ToHashSet();
}
```

**Resultado**: Scope GLOBAL incluye todos los players del brand, no solo los creados por usuarios en scope.

## ?? Verificación de Datos

### Consulta SQL para verificar balances:

```sql
-- Verificar balances de BackofficeUsers
SELECT 
    "Id",
    "Username",
    "Role",
    "WalletBalance",
    "BrandId"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111';

-- Verificar balances de Players
SELECT 
    p."Id",
    p."Username",
    w."BalanceBigint",
    p."BrandId",
    p."CreatedByUserId"
FROM "Players" p
LEFT JOIN "Wallets" w ON w."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111';

-- Verificar jerarquía
SELECT 
  "Id",
    "Username",
    "Role",
    "ParentAdminId",
    "ParentCashierId",
    "HierarchyLevel",
    "HierarchyPath"
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111'
ORDER BY "HierarchyLevel", "Username";
```

## ?? Testing después del Fix

### 1. Con Scope TREE (ahora incluye todos)

```bash
curl -X GET "http://localhost:5000/api/v1/admin/dashboard/overview?scope=TREE" \
  -H "Cookie: bk.token=YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

**Esperado**:
```json
{
  "finanzas": {
 "fichas": {
      "balanceActual": 99000,  // $490 (SUPER_ADMIN) + $200 (Cashier1) + $300 (Cashier2) + $500 (Player) en centavos
      "breakdown": {
        "houseBalance": 49000, // $490 del SUPER_ADMIN
        "cashiersBalance": 50000,// $200 + $300 = $500
     "playersBalance": 50000    // $500 del player
      }
    }
  },
  "usuarios": {
  "jugadoresDirectos": X,  // Creados directamente por SUPER_ADMIN
    "totalJugadores": X,     // Todos los players del brand
  "agentesDirectos": X,    // Cashiers creados directamente
    "totalAgentes": X     // Todos los cashiers del brand
  }
}
```

### 2. Con Scope GLOBAL (explícito)

```bash
curl -X GET "http://localhost:5000/api/v1/admin/dashboard/overview?scope=GLOBAL" \
  -H "Cookie: bk.token=YOUR_TOKEN"
```

**Esperado**: Mismo resultado que TREE para SUPER_ADMIN.

### 3. Con Scope DIRECT

```bash
curl -X GET "http://localhost:5000/api/v1/admin/dashboard/overview?scope=DIRECT" \
  -H "Cookie: bk.token=YOUR_TOKEN"
```

**Esperado**: Solo usuarios/players creados **directamente** por el SUPER_ADMIN.

## ?? Debugging

Si aún ves datos en 0, verifica:

### 1. **Los balances existen en la BD**

```sql
-- Total esperado de balances
SELECT 
    SUM("WalletBalance") as total_backoffice_balance
FROM "BackofficeUsers"
WHERE "BrandId" = '11111111-1111-1111-1111-111111111111';

SELECT 
    SUM(w."BalanceBigint") as total_player_balance
FROM "Players" p
JOIN "Wallets" w ON w."PlayerId" = p."Id"
WHERE p."BrandId" = '11111111-1111-1111-1111-111111111111';
```

### 2. **El BrandId en JWT token coincide**

```bash
# Decodifica el JWT token
echo "YOUR_JWT_TOKEN" | jwt decode -
```

Busca el claim `brand_id` y verifica que sea `11111111-1111-1111-1111-111111111111`.

### 3. **Logs de resolución de scope**

Los logs ahora incluyen:

```
Scope resolved: TREE, UserIds count: 3, BrandId: 11111111-1111-1111-1111-111111111111
GetPlayerIdsInScopeAsync: Using GLOBAL mode - 1 players in brand 11111111-1111-1111-1111-111111111111
```

Verifica que:
- `UserIds count` sea > 1 (incluye SUPER_ADMIN + cashiers)
- `players in brand` sea > 0

### 4. **Frontend debe usar scope correcto**

```typescript
// En el frontend, para SUPER_ADMIN:
const fetchDashboard = async () => {
  const response = await fetch('/api/v1/admin/dashboard/overview?scope=TREE', {
    credentials: 'include' // Incluye cookie con JWT
  });
  
  const data = await response.json();
  console.log('Dashboard data:', data);
};
```

## ?? Flujo de Datos Esperado

### Para SUPER_ADMIN con scope TREE:

1. **ResolveScopeAsync**:
   - Intenta obtener descendientes con `HierarchyService`
   - Si count == 1 (solo el SUPER_ADMIN), fallback a GLOBAL
   - Retorna todos los `BackofficeUsers` del brand en `userIds`

2. **GetPlayerIdsInScopeAsync**:
 - Si `userIds.Count > 50`, usa modo GLOBAL
   - Retorna **todos** los `Players` del brand

3. **CalculateCurrentBalanceAsync**:
   - `houseBalance`: Suma `WalletBalance` de SUPER_ADMIN y BRAND_ADMIN en `userIds`
   - `cashiersBalance`: Suma `WalletBalance` de CASHIER en `userIds`
   - `playersBalance`: Suma `BalanceBigint` de `Wallets` de players en `playerIds`

## ?? Ejemplo Real

### Datos en BD:

| Usuario | Role | WalletBalance | Notas |
|---------|------|---------------|-------|
| superadmin | SUPER_ADMIN | $490 (49000¢) | Usuario principal |
| cashier1 | CASHIER | $200 (20000¢) | Cajero 1 |
| cashier2 | CASHIER | $300 (30000¢) | Cajero 2 |
| player1 | PLAYER | $500 (50000¢) | Jugador 1 |

### Response Esperado:

```json
{
  "finanzas": {
    "fichas": {
 "balanceActual": 99000,  // $990 total
      "deltaDelDia": 0,        // Sin transacciones hoy
      "breakdown": {
        "houseBalance": 49000,   // SUPER_ADMIN
        "cashiersBalance": 50000,  // Cashier1 + Cashier2
        "playersBalance": 50000    // Player1
      }
    },
    "cargas": {
      "total": 0,    // Sin cargas en el período
      "count": 0,
      "promedio": 0
    },
    "depositosA": {
   "total": 0,    // Sin MINTs en el período
      "count": 0,
      "promedio": 0
    },
    "retiros": {
      "total": 0,    // Sin retiros en el período
      "count": 0,
      "promedio": 0
    }
  },
  "usuarios": {
    "jugadoresDirectos": 0,  // Si no fueron creados por SUPER_ADMIN
    "agentesDirectos": 0,    // Si no tienen ParentAdminId = SUPER_ADMIN
    "totalJugadores": 1,     // player1
    "totalAgentes": 2,       // cashier1 + cashier2
    "breakdown": {
 "jugadoresActivos": 1,
      "jugadoresInactivos": 0,
      "agentesPorNivel": {
      "nivel0": 2  // Ambos cashiers en nivel 0 si no tienen parent
 }
    }
  },
  "casino": {
    "jugado": 0,     // Sin apuestas en el período
    "pagado": 0,
    "netwin": 0,
    "comisionPorcentaje": 0,
    "comision": 0,
    "totalAPagar": 0,
    "kpIs": {
      "holdPercentage": 0,
      "rondasTotales": 0,
    "apuestaPromedio": 0,
      "jugadoresActivos": 0
    }
  },
  "alertas": {
    "alertas": [],
 "estadoOperativo": {
      "cajerosActivos": 0,   // Sin transacciones en últimas 24h
      "jugadoresOnline": 0,  // Sin sesiones abiertas
      "floatTotal": 50000,   // $500 total de cashiers
      "transaccionesPendientes": 0
    }
  }
}
```

## ?? Notas Importantes

1. **Cargas/Depósitos/Retiros en 0** es normal si no hay transacciones en el período (hoy).
2. **Casino en 0** es normal si no hay apuestas registradas en `Ledger`.
3. **Jugadores Directos en 0** es normal si no fueron creados con `CreatedByUserId = SUPER_ADMIN`.

## ?? Próximos Pasos

Si después del fix aún ves problemas:

1. **Ejecuta las queries SQL de verificación** para confirmar los balances
2. **Revisa los logs** de la aplicación para ver el scope resuelto
3. **Crea transacciones de prueba** para ver cargas/depósitos/retiros
4. **Crea apuestas de prueba** para ver datos de casino

---

**Fix implementado**: 2025-01-22  
**Archivos modificados**: `DashboardService.cs`
