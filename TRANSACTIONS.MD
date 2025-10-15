# API Administrativa de Transacciones Unificadas

## Descripción General

Los endpoints administrativos de transacciones han sido **unificados** para usar el mismo sistema que el gateway. Ahora todas las transacciones (backoffice y gateway) se registran en `Player.WalletBalance` y `WalletTransactions` con soporte completo para `TransactionType`.

**Base URL**: `/api/v1/admin/transactions`  
**Autenticación**: JWT Bearer Token (backoffice users)  
**Autorización**: Roles `SUPER_ADMIN`, `BRAND_ADMIN`, `CASHIER`

---

## 📋 Endpoints Disponibles

### 1. **POST** `/api/v1/admin/transactions`
**Crear Transacción**

Crea una nueva transacción usando el sistema unificado con soporte completo para `TransactionType`.

#### Request Body - MINT (Crear dinero)
```json
{
  "fromUserId": null,
  "fromUserType": null,
  "toUserId": "550e8400-e29b-41d4-a716-446655440000",
  "toUserType": "PLAYER",
  "amount": 100.50,
  "transactionType": "MINT",
  "idempotencyKey": "mint-001",
  "description": "Initial player funds"
}
```

#### Request Body - TRANSFER (Transferencia)
```json
{
  "fromUserId": "650e8400-e29b-41d4-a716-446655440001",
  "fromUserType": "BACKOFFICE",
  "toUserId": "550e8400-e29b-41d4-a716-446655440000",
  "toUserType": "PLAYER",
  "amount": 50.00,
  "transactionType": "TRANSFER",
  "idempotencyKey": "transfer-001",
  "description": "Cashier to player transfer"
}
```

#### Campos del Request
| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `fromUserId` | `guid?` | ❌ | ID usuario origen (null para MINT) |
| `fromUserType` | `string?` | ❌ | Tipo origen: "BACKOFFICE" o "PLAYER" (null para MINT) |
| `toUserId` | `guid` | ✅ | ID usuario destino |
| `toUserType` | `string` | ✅ | Tipo destino: "BACKOFFICE" o "PLAYER" |
| `amount` | `decimal` | ✅ | Monto (en formato decimal, ej: 100.50) |
| `transactionType` | `enum` | ✅ | Tipo de transacción (ver tipos disponibles) |
| `idempotencyKey` | `string` | ✅ | Clave de idempotencia (único por transacción) |
| `description` | `string?` | ❌ | Descripción opcional |

#### Tipos de Transacción Disponibles
| TransactionType | Descripción | Autorización |
|-----------------|-------------|--------------|
| `MINT` | Crear dinero | Solo SUPER_ADMIN |
| `TRANSFER` | Transferencia | BRAND_ADMIN, CASHIER |
| `DEPOSIT` | Depósito | BRAND_ADMIN, CASHIER |
| `WITHDRAWAL` | Retiro | BRAND_ADMIN, CASHIER |
| `BONUS` | Bonificación | BRAND_ADMIN |
| `ADJUSTMENT` | Ajuste manual | BRAND_ADMIN |
| `BET` | Apuesta (solo para referencia) | Sistema |
| `WIN` | Ganancia (solo para referencia) | Sistema |

#### Response (201 Created)
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "brandId": "550e8400-e29b-41d4-a716-446655440001",
  "type": "MINT",
  "fromUserId": null,
  "fromUserType": null,
  "fromUsername": null,
  "previousBalanceFrom": null,
  "newBalanceFrom": null,
  "toUserId": "550e8400-e29b-41d4-a716-446655440000",
  "toUserType": "PLAYER",
  "toUsername": "john_doe",
  "previousBalanceTo": 50.25,
  "newBalanceTo": 150.75,
  "amount": 100.50,
  "description": "Initial player funds",
  "transactionType": "MINT",
  "createdByUserId": "admin-user-id",
  "createdByUsername": "admin_user",
  "createdByRole": "SUPER_ADMIN",
  "idempotencyKey": "admin-mint-001",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Errores Posibles
- **400**: Validación fallida, brand no resuelto
- **403**: Sin permisos (ej: CASHIER intentando MINT)
- **404**: Jugador no encontrado
- **409**: Saldo insuficiente

---

### 2. **GET** `/api/v1/admin/transactions`
**Listar Transacciones**

Obtiene un listado paginado de transacciones con filtros avanzados.

#### Query Parameters
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `page` | `int` | ❌ | Número de página (default: 1) |
| `pageSize` | `int` | ❌ | Elementos por página (default: 20, max: 100) |
| `playerId` | `guid?` | ❌ | Filtrar por jugador específico |
| `transactionType` | `enum?` | ❌ | Filtrar por tipo de transacción |
| `fromDate` | `datetime?` | ❌ | Fecha desde (ISO 8601) |
| `toDate` | `datetime?` | ❌ | Fecha hasta (ISO 8601) |
| `externalRef` | `string?` | ❌ | Buscar por referencia externa |
| `globalScope` | `bool` | ❌ | Ver todas las brands (solo SUPER_ADMIN) |

#### Ejemplos de Consulta
```
GET /api/v1/admin/transactions?page=1&pageSize=10
GET /api/v1/admin/transactions?transactionType=MINT
GET /api/v1/admin/transactions?playerId=550e8400-e29b-41d4-a716-446655440000
GET /api/v1/admin/transactions?fromDate=2024-01-01&toDate=2024-01-31
GET /api/v1/admin/transactions?globalScope=true  // Solo SUPER_ADMIN
```

#### Response (200 OK)
```json
{
  "transactions": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "brandId": "550e8400-e29b-41d4-a716-446655440001",
      "type": "MINT",
      "fromUserId": null,
      "fromUserType": null,
      "fromUsername": null,
      "previousBalanceFrom": null,
      "newBalanceFrom": null,
      "toUserId": "550e8400-e29b-41d4-a716-446655440000",
      "toUserType": "PLAYER",
      "toUsername": "john_doe",
      "previousBalanceTo": 50.25,
      "newBalanceTo": 150.75,
      "amount": 100.50,
      "description": "Initial player funds",
      "transactionType": "MINT",
      "createdByUserId": "admin-user-id",
      "createdByUsername": "admin_user",
      "createdByRole": "SUPER_ADMIN",
      "idempotencyKey": "admin-mint-001",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

### 3. **POST** `/api/v1/admin/transactions/rollback`
**Revertir Transacción**

Revierte una transacción específica usando su `externalRef`.

#### Request Body
```json
{
  "externalRef": "admin-mint-001"
}
```

#### Response (200 OK)
```json
{
  "id": "rollback-transaction-id",
  "brandId": "550e8400-e29b-41d4-a716-446655440001",
  "type": "ROLLBACK",
  "fromUserId": "550e8400-e29b-41d4-a716-446655440000",
  "fromUserType": "PLAYER",
  "fromUsername": "john_doe",
  "previousBalanceFrom": 150.75,
  "newBalanceFrom": 50.25,
  "toUserId": null,
  "toUserType": "HOUSE",
  "toUsername": "System",
  "previousBalanceTo": 0,
  "newBalanceTo": 0,
  "amount": 100.50,
  "description": "Rollback of transaction admin-mint-001",
  "transactionType": "ROLLBACK",
  "createdByUserId": "admin-user-id",
  "createdByUsername": "admin_user",
  "createdByRole": "BRAND_ADMIN",
  "idempotencyKey": "rollback-admin-mint-001-uuid",
  "createdAt": "2024-01-15T11:00:00Z"
}
```

#### Errores Posibles
- **404**: Transacción original no encontrada
- **409**: Transacción ya revertida previamente

---

### 4. **GET** `/api/v1/admin/users/{userId}/balance`
**Obtener Balance de Usuario**

Obtiene el balance actual de cualquier usuario (PLAYER o BACKOFFICE).

#### Path Parameters
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `userId` | `guid` | ID del usuario |

#### Query Parameters
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `userType` | `string` | ✅ | Tipo de usuario: `PLAYER` o `BACKOFFICE` |

#### Ejemplos
```
GET /api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000/balance?userType=PLAYER
GET /api/v1/admin/users/550e8400-e29b-41d4-a716-446655440001/balance?userType=BACKOFFICE
```

#### Response (200 OK)
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userType": "PLAYER",
  "username": "john_doe",
  "balance": 150.75
}
```

#### Errores Posibles
- **400**: `userType` inválido (debe ser PLAYER o BACKOFFICE)
- **404**: Usuario no encontrado
- **403**: Sin permisos para ver el usuario

---

## 🔒 Autorización y Scope

### Roles y Permisos
| Rol | Permisos |
|-----|----------|
| `SUPER_ADMIN` | Todas las operaciones, acceso global |
| `BRAND_ADMIN` | Operaciones en su brand, excepto MINT |
| `CASHIER` | Transferencias básicas en su brand |

### Scope por Brand
- **SUPER_ADMIN**: Puede usar `globalScope=true` para ver todas las brands
- **BRAND_ADMIN/CASHIER**: Solo ven transacciones de su brand

### Transacciones Especiales
- **MINT**: Solo SUPER_ADMIN puede crear dinero
- **ROLLBACK**: Cualquier rol puede revertir transacciones de su scope

---

## 🚀 Ejemplos de Integración

### JavaScript/TypeScript
```javascript
// Crear transacción MINT (solo SUPER_ADMIN)
const createMint = async (toUserId, toUserType, amount) => {
  const response = await fetch('/api/v1/admin/transactions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      fromUserId: null,
      fromUserType: null,
      toUserId,
      toUserType,
      amount,
      transactionType: 'MINT',
      idempotencyKey: `mint-${Date.now()}`,
      description: 'Initial funds'
    })
  });
  
  return await response.json();
};

// Crear transacción TRANSFER (BRAND_ADMIN/CASHIER)
const createTransfer = async (fromUserId, fromUserType, toUserId, toUserType, amount) => {
  const response = await fetch('/api/v1/admin/transactions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      fromUserId,
      fromUserType,
      toUserId,
      toUserType,
      amount,
      transactionType: 'TRANSFER',
      idempotencyKey: `transfer-${Date.now()}`,
      description: 'Manual transfer'
    })
  });
  
  return await response.json();
};

// Listar transacciones con filtros
const getTransactions = async (filters = {}) => {
  const params = new URLSearchParams(filters);
  const response = await fetch(`/api/v1/admin/transactions?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return await response.json();
};

// Obtener balance de jugador
const getPlayerBalance = async (playerId) => {
  const response = await fetch(
    `/api/v1/admin/users/${playerId}/balance?userType=PLAYER`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  
  return await response.json();
};
```

### C# Client
```csharp
public class AdminTransactionClient
{
    private readonly HttpClient _httpClient;
    
    // MINT: Crear dinero
    public async Task<AdminTransactionResponse> CreateMintAsync(
        Guid toUserId, string toUserType, decimal amount)
    {
        var request = new CreateAdminTransactionRequest(
            FromUserId: null,
            FromUserType: null,
            ToUserId: toUserId,
            ToUserType: toUserType,
            Amount: amount,
            TransactionType: TransactionType.MINT,
            IdempotencyKey: $"mint-{Guid.NewGuid()}",
            Description: "MINT transaction"
        );
            
        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/admin/transactions", request);
            
        return await response.Content.ReadFromJsonAsync<AdminTransactionResponse>();
    }
    
    // TRANSFER: Transferir entre usuarios
    public async Task<AdminTransactionResponse> CreateTransferAsync(
        Guid fromUserId, string fromUserType,
        Guid toUserId, string toUserType, decimal amount)
    {
        var request = new CreateAdminTransactionRequest(
            FromUserId: fromUserId,
            FromUserType: fromUserType,
            ToUserId: toUserId,
            ToUserType: toUserType,
            Amount: amount,
            TransactionType: TransactionType.TRANSFER,
            IdempotencyKey: $"transfer-{Guid.NewGuid()}",
            Description: "Transfer transaction"
        );
            
        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/admin/transactions", request);
            
        return await response.Content.ReadFromJsonAsync<AdminTransactionResponse>();
    }
    
    public async Task<GetAdminTransactionsResponse> GetTransactionsAsync(
        int page = 1, TransactionType? type = null)
    {
        var query = $"?page={page}";
        if (type.HasValue) query += $"&transactionType={type}";
        
        return await _httpClient.GetFromJsonAsync<GetAdminTransactionsResponse>(
            $"/api/v1/admin/transactions{query}");
    }
}
```

---

## 📊 Casos de Uso Comunes

### 1. **Dar Fondos Iniciales a Jugador** (SUPER_ADMIN - MINT)
```json
POST /api/v1/admin/transactions
{
  "fromUserId": null,
  "fromUserType": null,
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 1000.00,
  "transactionType": "MINT",
  "idempotencyKey": "mint-welcome-001",
  "description": "Welcome bonus"
}
```

### 2. **Transferencia de Cajero a Jugador** (BRAND_ADMIN/CASHIER)
```json
POST /api/v1/admin/transactions
{
  "fromUserId": "cashier-id",
  "fromUserType": "BACKOFFICE",
  "toUserId": "player-id",
  "toUserType": "PLAYER",
  "amount": 50.00,
  "transactionType": "TRANSFER",
  "idempotencyKey": "transfer-001",
  "description": "Cashier to player"
}
```

### 3. **Transferencia entre Jugadores** (BRAND_ADMIN)
```json
POST /api/v1/admin/transactions
{
  "fromUserId": "player-id-1",
  "fromUserType": "PLAYER",
  "toUserId": "player-id-2",
  "toUserType": "PLAYER",
  "amount": 25.00,
  "transactionType": "TRANSFER",
  "idempotencyKey": "p2p-transfer-001",
  "description": "Player to player transfer"
}
```

### 3. **Consultar Transacciones de Hoy**
```
GET /api/v1/admin/transactions?fromDate=2024-01-15T00:00:00Z&toDate=2024-01-15T23:59:59Z
```

### 4. **Ver Solo Transacciones MINT**
```
GET /api/v1/admin/transactions?transactionType=MINT
```

---

## ⚠️ Notas Importantes

1. **Idempotencia**: El campo `idempotencyKey` es **REQUERIDO** y debe ser único por transacción
2. **Montos**: Siempre en formato decimal (100.50, no en centavos)
3. **Fechas**: Formato ISO 8601 (2024-01-15T10:30:00Z)
4. **Scope**: Respeta automáticamente el scope por brand según el rol
5. **Auditoría**: Todas las transacciones registran balances antes/después de AMBOS usuarios
6. **MINT**: `fromUserId` y `fromUserType` deben ser `null` para crear dinero
7. **TRANSFER**: Ambos usuarios (from y to) deben existir y estar en el mismo brand (excepto SUPER_ADMIN)
8. **UserTypes**: Siempre en mayúsculas: `"PLAYER"` o `"BACKOFFICE"`

---

## 🔗 Endpoints Relacionados

- **Gateway**: `/api/v1/gateway/*` (para proveedores de juegos)
- **Usuarios**: `/api/v1/admin/users/*` (gestión de usuarios)
- **Players**: `/api/v1/admin/players/*` (gestión específica de jugadores)

---

*Documentación actualizada para la API unificada de transacciones administrativas*