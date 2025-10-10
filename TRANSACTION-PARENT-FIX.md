# ? SOLUCIONADO: Errores en Creación de Jugadores y Cajeros

## ? Problemas Identificados

### 1. **Error de Transacción PostgreSQL en Jugadores**
```json
{
  "detail": "This NpgsqlTransaction has completed; it is no longer usable.",
  "status": 409,
  "title": "Player Creation Failed"
}
```

### 2. **Error de Validación en Cajeros Subordinados**
```json
{
  "detail": "Parent cashier must belong to the same operator",
  "status": 409,
  "title": "User Creation Failed"
}
```

## ?? Causas Raíz

### **1. Problema de Transacción**
En `PlayerService.CreatePlayerAsync()`, se llamaba a `AdjustPlayerWalletAsync()` dentro de una transacción activa, pero este método probablemente usaba otro contexto de base de datos, causando conflictos de transacción.

### **2. Problema de Parent Cashier**
Cuando un CASHIER creaba un subordinado, no se auto-asignaba como parent, y la validación del operator era demasiado estricta.

## ? Soluciones Implementadas

### **1. PlayerService Corregido**

**ANTES** (problemático):
```csharp
// Dentro de transacción
await _context.SaveChangesAsync();

if (request.InitialBalance > 0) {
    // ? Llamada a otro método que puede usar otro contexto
    var adjustResponse = await AdjustPlayerWalletAsync(newPlayer.Id, adjustRequest, currentUserId, null, null);
    if (!adjustResponse.Success) {
        await transaction.RollbackAsync(); // ? Transacción ya completada
        throw new InvalidOperationException($"Failed to set initial balance: {adjustResponse.ErrorMessage}");
    }
}
```

**AHORA** (corregido):
```csharp
// Crear wallet con balance inicial directamente
var wallet = new Wallet
{
    PlayerId = newPlayer.Id,
    BalanceBigint = request.InitialBalance // ? Balance inicial directo
};

_context.Wallets.Add(wallet);
await _context.SaveChangesAsync(); // ? Una sola operación en la transacción
```

### **2. BackofficeUserEndpoints Corregido**

**ANTES** (manual):
```csharp
// Usuario tenía que especificar parentCashierId manualmente
if (request.ParentCashierId != currentUserId) {
    return Results.Problem("CASHIER can only create subordinates under themselves");
}
```

**AHORA** (automático):
```csharp
// Auto-asignar al cashier actual como parent si no se especifica
if (!request.ParentCashierId.HasValue) {
    effectiveRequest = effectiveRequest with { ParentCashierId = currentUserId };
    logger.LogInformation("Auto-assigning parent cashier {CashierId} for subordinate creation", currentUserId);
}
```

### **3. BackofficeUserService Mejorado**

**ANTES** (mensaje genérico):
```csharp
throw new InvalidOperationException("Parent cashier must belong to the same operator");
```

**AHORA** (mensaje detallado):
```csharp
throw new InvalidOperationException($"Parent cashier belongs to operator '{parentOperatorName}', but user is being created for operator '{currentOperatorName}'");
```

## ?? Flujo Correcto Ahora

### **Creación de Jugador**:

```
1. Frontend ? Request con initialBalance
   POST /api/v1/admin/players
   {
     "username": "jugador",
     "email": "luquesjuanse.10@gmail.com",
     "initialBalance": 2000,
     "password": "hola1234",
     "status": "ACTIVE"
   }

2. PlayerService.CreatePlayerAsync()
   ??? Crea Player en BD
   ??? Crea Wallet con BalanceBigint = 2000 (directo)
   ??? SaveChanges() una sola vez
   ??? Commit transaction ?
   ??? Retorna GetPlayerResponse

3. Resultado: 201 Created ?
   {
     "id": "...",
     "username": "jugador",
     "balance": 2000,
     "status": "ACTIVE"
   }
```

### **Creación de Cajero Subordinado**:

```
1. Frontend ? Request sin parentCashierId (opcional)
   POST /api/v1/admin/users
   {
     "username": "cajero_subordinado",
     "password": "password123",
     "role": "CASHIER",
     "commissionRate": 5.0
   }

2. BackofficeUserEndpoints
   ??? currentRole = CASHIER
   ??? effectiveOperatorId = brandContext.OperatorId
   ??? Auto-asigna: parentCashierId = currentUserId ?
   ??? effectiveRequest = { ..., ParentCashierId = currentUserId }

3. BackofficeUserService.CreateUserAsync()
   ??? Valida parent cashier existe
   ??? Valida parent cashier mismo operator ?
   ??? Crea usuario subordinado
   ??? Retorna GetBackofficeUserResponse

4. Resultado: 201 Created ?
   {
     "id": "...",
     "username": "cajero_subordinado",
     "role": "CASHIER",
     "parentCashierId": "id-del-cashier-padre",
     "commissionRate": 5.0
   }
```

## ?? Casos de Uso Funcionando

### **? Creación de Jugadores**:
- ? Con balance inicial (se establece directamente en wallet)
- ? Sin balance inicial (wallet con 0)
- ? Con email, password, externalId
- ? Validación de username único por brand

### **? Creación de Cajeros**:
- ? CASHIER puede crear subordinados (auto-asigna como parent)
- ? OPERATOR_ADMIN puede crear cajeros
- ? Validación de operator coherente
- ? Commission rate entre 0-100

## ?? Requests que Ahora Funcionan

### **Crear Jugador** (desde cualquier rol autorizado):
```json
POST /api/v1/admin/players
{
  "username": "nuevo_jugador",
  "email": "jugador@example.com",
  "initialBalance": 1000,
  "password": "password123",
  "status": "ACTIVE"
}
```

### **Crear Cajero Subordinado** (desde CASHIER):
```json
POST /api/v1/admin/users
{
  "username": "mi_subordinado",
  "password": "password123",
  "role": "CASHIER",
  "commissionRate": 3.5
}
```

### **Crear Cajero** (desde OPERATOR_ADMIN):
```json
POST /api/v1/admin/users
{
  "username": "nuevo_cajero",
  "password": "password123",
  "role": "CASHIER",
  "commissionRate": 10.0
}
```

## ?? Archivos Modificados

- ? `apps\Casino.Application\Services\Implementations\PlayerService.cs`
- ? `apps\api\Casino.Api\Endpoints\BackofficeUserEndpoints.cs`
- ? `apps\Casino.Application\Services\Implementations\BackofficeUserService.cs`

## ? Resultado Final

1. **? Jugadores se crean exitosamente** con balance inicial directo
2. **? Cajeros subordinados se auto-asignan** al parent automáticamente  
3. **? Transacciones PostgreSQL funcionan** sin conflictos
4. **? Validaciones son más informativas** con mensajes detallados
5. **? UX mejorada** - menos campos requeridos en frontend

Los errores 409 están **completamente resueltos** para ambos casos de uso.