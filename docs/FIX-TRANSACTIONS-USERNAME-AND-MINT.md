# ?? Fix: Transacciones - `fromUsername` NULL y DEPOSIT vs MINT

## ?? **Problemas Reportados**

### Problema 1: ? `fromUsername` = null y `toUsername` = "Unknown"

**Transacción MINT (SUPER_ADMIN ? localadmin)**:
```json
{
  "fromUserId": null,
  "fromUserType": null,
  "fromUsername": null, // ? Correcto para MINT
  "toUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
  "toUserType": "BACKOFFICE",
  "toUsername": "Unknown", // ? Debería ser "localadmin"
  "transactionType": "MINT"
}
```

**Transacción TRANSFER (localadmin ? cajero)**:
```json
{
  "fromUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
  "fromUserType": "BACKOFFICE",
  "fromUsername": null, // ? Debería ser "localadmin"
  "toUserId": "b96d9861-f7e1-41b8-bb50-d85af7d7c1fe",
  "toUserType": "BACKOFFICE",
  "toUsername": "Unknown", // ? Debería ser nombre del cajero
  "transactionType": "TRANSFER"
}
```

**Causa**: 
1. `GetTransactionsAsync` (línea ~163): Solo buscaba usernames en `Players`, ignorando `BackofficeUsers`
2. `MapTransactionToAdminResponseAsync` (línea ~566): Usaba `GetUsernameAsync` individual que retornaba "Unknown"

### Problema 2: ? SUPER_ADMIN usa DEPOSIT (debería ser MINT)

**Causa**: El frontend está enviando `transactionType: "DEPOSIT"` en lugar de `"MINT"` cuando el SUPER_ADMIN crea dinero desde la nada.

---

## ? **Soluciones Implementadas**

### **Fix 1A: `fromUsername` NULL en GetTransactionsAsync**

**Ubicación**: `AdminTransactionService.cs` ? `GetTransactionsAsync()` (línea ~163)

#### **ANTES** (? Incorrecto):

```csharp
// Solo buscaba en Players
var playerIds = transactions
    .SelectMany(t => new[] { t.FromUserId, t.ToUserId })
    .Where(id => id.HasValue)
    .Select(id => id!.Value)
    .Distinct()
    .ToList();

var players = await _context.Players
    .Where(p => playerIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id, p => p.Username);

// ...

var transactionResponses = transactions.Select(t =>
{
    var fromUsername = t.FromUserId.HasValue && players.TryGetValue(t.FromUserId.Value, out var from) 
   ? from : null; // ? Siempre null si era BACKOFFICE

    var toUsername = players.TryGetValue(t.ToUserId, out var to) 
 ? to : "Unknown"; // ? Siempre "Unknown" si era BACKOFFICE
    
    return new AdminTransactionResponse(...);
});
```

**Problema**: Solo buscaba usernames en `Players`, ignorando `BackofficeUsers`.

#### **AHORA** (? Correcto):

```csharp
// FIX: Obtener información de TODOS los usuarios (PLAYER y BACKOFFICE)
var playerIds = transactions
    .SelectMany(t => new[] { t.FromUserId, t.ToUserId })
    .Where(id => id.HasValue)
    .Select(id => id!.Value)
    .Distinct()
    .ToList();

var players = await _context.Players
    .Where(p => playerIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id, p => p.Username);

// FIX: Obtener backoffice users que aparecen en FROM o TO
var backofficeUserIds = transactions
    .SelectMany(t => new[]
    {
      t.FromUserType == "BACKOFFICE" ? t.FromUserId : null,
      t.ToUserType == "BACKOFFICE" ? (Guid?)t.ToUserId : null
    })
    .Where(id => id.HasValue)
    .Select(id => id!.Value)
    .Distinct()
    .ToList();

var backofficeUsers = await _context.BackofficeUsers
    .Where(u => backofficeUserIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.Username);

// ...

var transactionResponses = transactions.Select(t =>
{
    // FIX: Obtener fromUsername correctamente según el tipo
    string? fromUsername = null;
    if (t.FromUserId.HasValue)
  {
        if (t.FromUserType == "BACKOFFICE")
        {
        backofficeUsers.TryGetValue(t.FromUserId.Value, out fromUsername);
        }
      else if (t.FromUserType == "PLAYER")
 {
            players.TryGetValue(t.FromUserId.Value, out fromUsername);
     }
    }

    // FIX: Obtener toUsername correctamente según el tipo
    string? toUsername = null;
    if (t.ToUserType == "BACKOFFICE")
    {
        backofficeUsers.TryGetValue(t.ToUserId, out toUsername);
 }
    else if (t.ToUserType == "PLAYER")
    {
      players.TryGetValue(t.ToUserId, out toUsername);
    }
    
    return new AdminTransactionResponse(...);
});
```

**Beneficio**:
- ? `fromUsername` se obtiene correctamente para BACKOFFICE y PLAYER
- ? `toUsername` se obtiene correctamente para BACKOFFICE y PLAYER
- ? Funciona para transacciones BACKOFFICE ? PLAYER, PLAYER ? BACKOFFICE, etc.

---

### **Fix 1B: `toUsername` = "Unknown" en MapTransactionToAdminResponseAsync**

**Ubicación**: `AdminTransactionService.cs` ? `MapTransactionToAdminResponseAsync()` (línea ~566)

#### **ANTES** (? Incorrecto):

```csharp
private async Task<AdminTransactionResponse> MapTransactionToAdminResponseAsync(
    WalletTransaction transaction, Guid actorUserId, BackofficeUserRole actorRole)
{
    // ? Consultas individuales por cada usuario (ineficiente + retorna "Unknown")
    var fromUsername = transaction.FromUserId.HasValue 
        ? await GetUsernameAsync(transaction.FromUserId.Value, transaction.FromUserType!) 
        : null;
    var toUsername = await GetUsernameAsync(transaction.ToUserId, transaction.ToUserType);
    
    var actor = await _context.BackofficeUsers.FindAsync(actorUserId);

    return new AdminTransactionResponse(...);
}

// GetUsernameAsync siempre retorna "Unknown" si no encuentra
private async Task<string> GetUsernameAsync(Guid userId, string userType)
{
    if (userType == "BACKOFFICE")
    {
   var user = await _context.BackofficeUsers.FindAsync(userId);
        return user?.Username ?? "Unknown"; // ? Siempre "Unknown" si no existe
    }
    // ...
}
```

**Problema**: 
- Hacía consultas individuales (N+1 queries)
- Retornaba "Unknown" sin logging
- No encontraba los usuarios correctamente

#### **AHORA** (? Correcto):

```csharp
private async Task<AdminTransactionResponse> MapTransactionToAdminResponseAsync(
    WalletTransaction transaction, Guid actorUserId, BackofficeUserRole actorRole)
{
    // ? FIX: Usar batch queries en lugar de consultas individuales
    string? fromUsername = null;
    string? toUsername = null;
    
    // Batch query para backoffice users
    var backofficeUserIds = new List<Guid>();
    if (transaction.FromUserType == "BACKOFFICE" && transaction.FromUserId.HasValue)
      backofficeUserIds.Add(transaction.FromUserId.Value);
    if (transaction.ToUserType == "BACKOFFICE")
        backofficeUserIds.Add(transaction.ToUserId);
    backofficeUserIds.Add(actorUserId); // Actor siempre es backoffice
    
    var backofficeUsers = await _context.BackofficeUsers
        .Where(u => backofficeUserIds.Contains(u.Id))
 .ToDictionaryAsync(u => u.Id, u => u.Username);
    
    // Batch query para players
    var playerIdsToQuery = new List<Guid>();
    if (transaction.FromUserType == "PLAYER" && transaction.FromUserId.HasValue)
        playerIdsToQuery.Add(transaction.FromUserId.Value);
    if (transaction.ToUserType == "PLAYER")
        playerIdsToQuery.Add(transaction.ToUserId);
    
    var players = await _context.Players
        .Where(p => playerIdsToQuery.Contains(p.Id))
      .ToDictionaryAsync(p => p.Id, p => p.Username);
    
    // ? Obtener fromUsername correctamente
    if (transaction.FromUserId.HasValue)
    {
        if (transaction.FromUserType == "BACKOFFICE")
        {
  if (!backofficeUsers.TryGetValue(transaction.FromUserId.Value, out fromUsername))
        {
            _logger.LogWarning("FromUsername not found - BACKOFFICE user: {UserId}", transaction.FromUserId.Value);
       fromUsername = null;
            }
      }
        else if (transaction.FromUserType == "PLAYER")
        {
        if (!players.TryGetValue(transaction.FromUserId.Value, out fromUsername))
         {
         _logger.LogWarning("FromUsername not found - PLAYER: {UserId}", transaction.FromUserId.Value);
       fromUsername = null;
            }
        }
    }
    
    // ? Obtener toUsername correctamente
    if (transaction.ToUserType == "BACKOFFICE")
    {
        if (!backofficeUsers.TryGetValue(transaction.ToUserId, out toUsername))
   {
        _logger.LogWarning("ToUsername not found - BACKOFFICE user: {UserId}", transaction.ToUserId);
  toUsername = "Unknown";
        }
    }
    else if (transaction.ToUserType == "PLAYER")
    {
        if (!players.TryGetValue(transaction.ToUserId, out toUsername))
   {
            _logger.LogWarning("ToUsername not found - PLAYER: {UserId}", transaction.ToUserId);
 toUsername = "Unknown";
    }
    }
    
    // ? Obtener actor username
    var actorUsername = backofficeUsers.TryGetValue(actorUserId, out var actor) 
        ? actor 
      : "Unknown";
    
    return new AdminTransactionResponse(..., fromUsername, ..., toUsername, ..., actorUsername, ...);
}
```

**Beneficio**:
- ? Usa **batch queries** (2 consultas en lugar de N)
- ? Logging cuando no encuentra usuarios (facilita debugging)
- ? `fromUsername` se obtiene correctamente para BACKOFFICE y PLAYER
- ? `toUsername` se obtiene correctamente para BACKOFFICE y PLAYER
- ? Más eficiente y detecta problemas de datos

---

## ? **Solución Implementada: DEPOSIT vs MINT** (?? Requiere cambio en Frontend)

**Problema**: El frontend actual envía esto cuando SUPER_ADMIN crea dinero:

```json
{
  "fromUserId": null,
  "fromUserType": null,
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 1000,
  "transactionType": "DEPOSIT", // ? INCORRECTO
  "idempotencyKey": "deposit-123",
  "description": "Envío de fondos desde superadmin"
}
```

**Solución**: El frontend debe enviar `"MINT"` cuando:
1. El usuario es SUPER_ADMIN
2. `fromUserId` es `null`
3. Se está "creando dinero desde la nada"

#### **Lógica Correcta en Frontend**:

```typescript
// utils/transactionTypes.ts
export function determineTransactionType(
  fromUserId: string | null,
  fromUserType: string | null,
  currentUserRole: string
): string {
  // Si no hay origen, es MINT (crear dinero)
  if (!fromUserId && !fromUserType) {
    if (currentUserRole === 'SUPER_ADMIN') {
   return 'MINT'; // ? CORRECTO
    }
    throw new Error('Only SUPER_ADMIN can create MINT transactions');
  }
  
  // Si hay origen y destino, es TRANSFER
  return 'TRANSFER';
}

// components/SendFunds.tsx
const handleSendFunds = async () => {
  const transactionType = determineTransactionType(
    fromUserId,
    fromUserType,
    currentUser.role
  );
  
  await fetch('/api/v1/admin/transactions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      fromUserId: fromUserId || null,
      fromUserType: fromUserType || null,
      toUserId: selectedPlayer.id,
      toUserType: 'PLAYER',
      amount: amount,
      transactionType: transactionType, // ? "MINT" o "TRANSFER"
      idempotencyKey: `tx-${Date.now()}`,
      description: `Envío de fondos desde ${currentUser.username}`
    })
  });
};
```

---

## ?? **Resultado Esperado Después del Fix**

### **Transacción MINT** (SUPER_ADMIN ? localadmin)

```json
{
  "id": "ab1743be-634c-446d-9a29-0bae892ca64f",
  "brandId": "11111111-1111-1111-1111-111111111111",
  "type": "MINT",
  "fromUserId": null,
  "fromUserType": null,
  "fromUsername": null, // ? Correcto para MINT
  "toUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
  "toUserType": "BACKOFFICE",
  "toUsername": "localadmin", // ? FIX: Ahora aparece correctamente
  "amount": 1000000,
  "transactionType": "MINT",
  "createdByUsername": "superadmin"
}
```

### **Transacción TRANSFER** (localadmin ? cajero)

```json
{
  "id": "01c10305-e767-4c53-8cc9-422506334823",
  "brandId": "11111111-1111-1111-1111-111111111111",
  "type": "TRANSFER",
  "fromUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
  "fromUserType": "BACKOFFICE",
  "fromUsername": "localadmin", // ? FIX: Ahora aparece correctamente
  "toUserId": "b96d9861-f7e1-41b8-bb50-d85af7d7c1fe",
  "toUserType": "BACKOFFICE",
  "toUsername": "localcajero", // ? FIX: Ahora aparece correctamente
  "amount": 3000,
  "transactionType": "TRANSFER",
  "createdByUsername": "localadmin"
}
```

---

## ?? **Debugging**

Si después del fix todavía aparece "Unknown", revisa los logs:

```
[Warning] FromUsername not found - BACKOFFICE user: ea3080a9-64d6-479c-9500-73730333e3a5
[Warning] ToUsername not found - BACKOFFICE user: b96d9861-f7e1-41b8-bb50-d85af7d7c1fe
```

Esto indica que:
1. Los GUIDs no existen en la base de datos
2. Hay un problema con los datos de prueba

**Solución**: Verificar que los usuarios existan:

```sql
SELECT Id, Username, Role FROM BackofficeUsers 
WHERE Id IN ('ea3080a9-64d6-479c-9500-73730333e3a5', 'b96d9861-f7e1-41b8-bb50-d85af7d7c1fe');
```

---

## ?? **Resultado Esperado Después del Fix**

### **Transacciones GET** (después del fix backend)

```json
GET /api/v1/admin/transactions

{
  "transactions": [
    {
      "id": "b796f371-2ea8-4d14-b66e-055bea62a54a",
      "brandId": "11111111-1111-1111-1111-111111111111",
      "type": "TRANSFER",
      "fromUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
      "fromUserType": "BACKOFFICE",
      "fromUsername": "localadmin", // ? FIX: Ahora aparece correctamente
      "previousBalanceFrom": 46000,
      "newBalanceFrom": 43000,
      "toUserId": "d6c1a2c2-90c5-460b-b9c8-e240bc2c0947",
      "toUserType": "PLAYER",
      "toUsername": "localjugador2",
    "previousBalanceTo": 3000,
      "newBalanceTo": 6000,
      "amount": 3000,
 "description": "Envío de fondos desde localadmin",
   "transactionType": "TRANSFER",
      "createdByUserId": "ea3080a9-64d6-479c-9500-73730333e3a5",
      "createdByUsername": "localadmin",
      "createdByRole": "BRAND_ADMIN",
      "idempotencyKey": "deposit-1761188246618-3z8o8j1s2",
      "createdAt": "2025-10-23T02:57:27.720078Z"
    }
  ]
}
```

### **Transacciones MINT** (después del fix frontend)

```json
POST /api/v1/admin/transactions

{
  "fromUserId": null,
"fromUserType": null,
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 1000,
  "transactionType": "MINT", // ? CORRECTO
  "idempotencyKey": "mint-123",
  "description": "Envío de fondos desde superadmin"
}
```

**Response**:

```json
{
  "id": "new-transaction-id",
  "brandId": "11111111-1111-1111-1111-111111111111",
  "type": "MINT", // ? CORRECTO
  "fromUserId": null,
  "fromUserType": null,
  "fromUsername": null, // ? Correcto para MINT
  "previousBalanceFrom": null,
  "newBalanceFrom": null,
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "toUsername": "localjugador",
  "previousBalanceTo": 0,
  "newBalanceTo": 1000,
  "amount": 1000,
  "description": "Envío de fondos desde superadmin",
  "transactionType": "MINT", // ? CORRECTO
  "createdByUserId": "superadmin-id",
  "createdByUsername": "superadmin",
  "createdByRole": "SUPER_ADMIN",
  "idempotencyKey": "mint-123",
  "createdAt": "2025-01-23T12:00:00Z"
}
```

---

## ?? **Validación**

### **Prueba 1: Verificar `fromUsername` NULL**

```sh
curl -X GET "https://localhost:7182/api/v1/admin/transactions?page=1&pageSize=10" \
  -H "Cookie: bk.token.localhost_dev=TOKEN"
```

**Verificar**:
- ? `fromUsername` tiene valor cuando `fromUserType = "BACKOFFICE"`
- ? `toUsername` tiene valor cuando `toUserType = "BACKOFFICE"`

### **Prueba 2: Verificar MINT desde SUPER_ADMIN**

```sh
curl -X POST "https://localhost:7182/api/v1/admin/transactions" \
  -H "Content-Type: application/json" \
  -H "Cookie: bk.token.localhost_dev=TOKEN" \
  -d '{
    "fromUserId": null,
    "fromUserType": null,
    "toUserId": "player-id",
    "toUserType": "PLAYER",
    "amount": 1000,
"transactionType": "MINT",
    "idempotencyKey": "mint-test-123",
    "description": "Test MINT"
  }'
```

**Verificar**:
- ? `transactionType = "MINT"` en la respuesta
- ? `type = "MINT"` en la respuesta
- ? `fromUsername = null` (correcto para MINT)

---

## ?? **Tabla Comparativa: DEPOSIT vs MINT**

| Aspecto | DEPOSIT | MINT |
|---------|---------|------|
| **Origen** | Tiene `fromUserId` y `fromUserType` | ? `null` (sin origen) |
| **Destino** | Tiene `toUserId` y `toUserType` | ? Tiene destino |
| **Descripción** | "Depósito externo" | "Crear dinero desde la nada" |
| **Autorización** | BRAND_ADMIN, CASHIER | ? **Solo SUPER_ADMIN** |
| **Uso Correcto** | Depósito desde cuenta externa | Emisión de fondos iniciales |

### **Ejemplos de Uso**

#### **MINT** (Crear dinero - Solo SUPER_ADMIN):
```json
{
  "fromUserId": null,
  "fromUserType": null,
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 1000,
  "transactionType": "MINT"
}
```

#### **DEPOSIT** (Depósito externo - BRAND_ADMIN/CASHIER):
```json
{
  "fromUserId": "external-account-id",
  "fromUserType": "BACKOFFICE",
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 500,
  "transactionType": "DEPOSIT"
}
```

#### **TRANSFER** (Transferencia interna - Todos):
```json
{
  "fromUserId": "cashier-id",
  "fromUserType": "BACKOFFICE",
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 200,
  "transactionType": "TRANSFER"
}
```

---

## ?? **Resumen de Cambios**

### **Backend** (? Completado)
- [x] `AdminTransactionService.GetTransactionsAsync`: Buscar usernames en ambas tablas (Players + BackofficeUsers)
- [x] `AdminTransactionService.MapTransactionToAdminResponseAsync`: Usar batch queries y logging
- [x] `AdminTransactionService.GetUsernameAsync`: Agregar logging cuando no encuentra usuarios
- [x] Filtrar correctamente `backofficeUserIds` según `fromUserType` y `toUserType`
- [x] Mapear `fromUsername` y `toUsername` según el tipo de usuario

### **Frontend** (?? Pendiente)
- [ ] Crear función `determineTransactionType()`
- [ ] Usar `"MINT"` cuando SUPER_ADMIN crea dinero sin origen
- [ ] Usar `"TRANSFER"` cuando hay origen y destino
- [ ] Validar que solo SUPER_ADMIN pueda enviar transacciones MINT

---

## ?? **Documentación Relacionada**

- **API de Transacciones**: `docs/TRANSACTIONS.MD`
- **Tipos de Transacción**: `Casino.Domain.Enums.TransactionType`
- **Admin Endpoints**: `apps/api/Casino.Api/Endpoints/AdminTransactionEndpoints.cs`

---

**Archivo**: `apps/Casino.Application/Services/Implementations/AdminTransactionService.cs`  
**Líneas modificadas**: 
- ~163-230 (`GetTransactionsAsync` - batch query)
- ~455-475 (`GetUsernameAsync` - logging)
- ~566-650 (`MapTransactionToAdminResponseAsync` - batch query + logging)

**Fecha**: 2025-01-23  
**Versión**: 1.3
