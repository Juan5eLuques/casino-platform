# Casino Platform API - Documentaci�n Completa de Endpoints

## ?? **Resumen General**

Esta es una plataforma de casino multi-brand con autenticaci�n h�brida JWT+Cookies, separaci�n de contextos (Backoffice vs Players), y sistema de gesti�n completo.

### **Arquitectura de Autenticaci�n**
- **Backoffice**: JWT audience "backoffice", Cookie "bk.token" (Path: /admin)
- **Players**: JWT audience "player", Cookie "pl.token" (Path: /)
- **Roles**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (backoffice) / PLAYER (site)

### **Resoluci�n de Brand**
- **Por Host**: BrandResolverMiddleware identifica el brand por dominio
- **Context Injection**: Autom�tico en todos los endpoints basado en Host header

---

## ?? **AUTENTICACI�N Y AUTORIZACI�N**

### **Backoffice Authentication**

#### **POST /api/v1/admin/auth/login**
**Descripci�n**: Login para usuarios de backoffice
**Headers**: `Host: admin.{domain}` (requerido para brand resolution)

**Request:**
```json
{
  "username": "admin_mycasino",
  "password": "admin123"
}
```

**Response 200:**
```json
{
  "success": true,
  "user": {
    "id": "uuid",
    "username": "admin_mycasino",
    "role": "OPERATOR_ADMIN",
    "operatorId": "uuid",
    "status": "ACTIVE"
  },
  "expiresAt": "2024-01-01T08:00:00Z"
}
```

**Response 401:**
```json
{
  "success": false,
  "errorMessage": "Invalid credentials"
}
```

**Cookies Set**: `bk.token=<JWT>; HttpOnly; Secure; Path=/admin; Expires=8h`

#### **GET /api/v1/admin/auth/me**
**Descripci�n**: Obtener perfil del usuario actual
**Auth**: Bearer token o cookie bk.token

**Response 200:**
```json
{
  "id": "uuid",
  "username": "admin_mycasino",
  "role": "OPERATOR_ADMIN",
  "operatorId": "uuid",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-01T08:00:00Z"
}
```

#### **POST /api/v1/admin/auth/logout**
**Descripci�n**: Cerrar sesi�n de backoffice
**Auth**: Bearer token o cookie bk.token

**Response 200:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

### **Player Authentication**

#### **POST /api/v1/auth/login**
**Descripci�n**: Login para jugadores
**Headers**: `Host: {domain}` (requerido para brand resolution)

**Request:**
```json
{
  "username": "jugador1",
  "password": "demo123"
}
```

**Response 200:**
```json
{
  "success": true,
  "user": {
    "id": "uuid",
    "username": "jugador1",
    "email": "jugador1@mycasino.local",
    "brandId": "uuid",
    "status": "ACTIVE"
  },
  "expiresAt": "2024-01-01T08:00:00Z"
}
```

**Cookies Set**: `pl.token=<JWT>; HttpOnly; Secure; Path=/; Expires=8h`

#### **GET /api/v1/auth/me**
**Descripci�n**: Obtener perfil del jugador actual
**Auth**: Bearer token o cookie pl.token

**Response 200:**
```json
{
  "id": "uuid",
  "username": "jugador1",
  "email": "jugador1@mycasino.local",
  "brandId": "uuid",
  "status": "ACTIVE",
  "balance": 10000,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

#### **POST /api/v1/auth/logout**
**Descripci�n**: Cerrar sesi�n de jugador
**Auth**: Bearer token o cookie pl.token

**Response 200:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

## ?? **GESTI�N DE OPERADORES** (Admin)

### **GET /api/v1/admin/operators**
**Descripci�n**: Listar operadores con filtros y paginaci�n
**Auth**: SUPER_ADMIN
**Query Params**:
- `name`: string (filtro por nombre)
- `status`: ACTIVE|INACTIVE
- `page`: int (default: 1)
- `limit`: int (default: 20)

**Response 200:**
```json
{
  "operators": [
    {
      "id": "uuid",
      "name": "MiCasino Corp",
      "status": "ACTIVE",
      "brandsCount": 3,
      "usersCount": 12,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 1,
    "pages": 1
  }
}
```

### **POST /api/v1/admin/operators**
**Descripci�n**: Crear nuevo operador
**Auth**: SUPER_ADMIN

**Request:**
```json
{
  "name": "Nuevo Casino Corp",
  "status": "ACTIVE"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "name": "Nuevo Casino Corp",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### **PATCH /api/v1/admin/operators/{operatorId}**
**Descripci�n**: Actualizar operador
**Auth**: SUPER_ADMIN

**Request:**
```json
{
  "name": "Casino Corp Actualizado",
  "status": "INACTIVE"
}
```

---

## ?? **GESTI�N DE BRANDS** (Admin)

### **GET /api/v1/admin/brands**
**Descripci�n**: Listar brands con filtros
**Auth**: SUPER_ADMIN (ve todos), OPERATOR_ADMIN (solo de su operador)
**Query Params**:
- `operatorId`: uuid (filtro por operador)
- `status`: ACTIVE|INACTIVE|MAINTENANCE
- `domain`: string
- `page`: int (default: 1)
- `limit`: int (default: 20)

**Response 200:**
```json
{
  "brands": [
    {
      "id": "uuid",
      "operatorId": "uuid",
      "code": "mycasino",
      "name": "MiCasino",
      "domain": "mycasino.local",
      "adminDomain": "admin.mycasino.local",
      "locale": "es-ES",
      "status": "ACTIVE",
      "theme": {
        "primaryColor": "#1a73e8",
        "logo": "/assets/logo.png"
      },
      "settings": {
        "maxBetAmount": 10000,
        "currency": "EUR",
        "timezone": "Europe/Madrid"
      },
      "playersCount": 4,
      "gamesCount": 4,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 1,
    "pages": 1
  }
}
```

### **POST /api/v1/admin/brands**
**Descripci�n**: Crear nuevo brand
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Request:**
```json
{
  "operatorId": "uuid",
  "code": "newcasino",
  "name": "New Casino",
  "domain": "newcasino.local",
  "adminDomain": "admin.newcasino.local",
  "corsOrigins": ["http://localhost:3000", "https://newcasino.local"],
  "locale": "en-US",
  "status": "ACTIVE",
  "theme": {
    "primaryColor": "#2563eb",
    "secondaryColor": "#64748b",
    "logo": "/assets/logo.png"
  },
  "settings": {
    "maxBetAmount": 5000,
    "currency": "USD",
    "timezone": "America/New_York"
  }
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "operatorId": "uuid",
  "code": "newcasino",
  "name": "New Casino",
  "domain": "newcasino.local",
  "adminDomain": "admin.newcasino.local",
  "locale": "en-US",
  "status": "ACTIVE",
  "theme": { /* ... */ },
  "settings": { /* ... */ },
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### **PATCH /api/v1/admin/brands/{brandId}**
**Descripci�n**: Actualizar brand
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo su operador)

**Request:**
```json
{
  "name": "Casino Actualizado",
  "status": "MAINTENANCE",
  "theme": {
    "primaryColor": "#dc2626"
  },
  "settings": {
    "maxBetAmount": 15000
  }
}
```

### **PUT /api/v1/admin/brands/{brandId}/providers/{providerCode}**
**Descripci�n**: Configurar proveedor para brand
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Request:**
```json
{
  "secret": "mi_secreto_hmac_super_seguro_32_chars",
  "allowNegativeOnRollback": false,
  "meta": {
    "webhookUrl": "https://mycasino.local/api/v1/gateway",
    "timeout": 30
  }
}
```

---

## ?? **GESTI�N DE USUARIOS BACKOFFICE** (Admin)

### **GET /api/v1/admin/users**
**Descripci�n**: Listar usuarios de backoffice
**Auth**: SUPER_ADMIN (ve todos), OPERATOR_ADMIN (solo de su operador)
**Query Params**:
- `operatorId`: uuid
- `role`: SUPER_ADMIN|OPERATOR_ADMIN|CASHIER
- `status`: ACTIVE|INACTIVE|SUSPENDED
- `search`: string (buscar por username)
- `page`: int (default: 1)
- `limit`: int (default: 20)

**Response 200:**
```json
{
  "users": [
    {
      "id": "uuid",
      "operatorId": "uuid",
      "username": "admin_mycasino",
      "role": "OPERATOR_ADMIN",
      "status": "ACTIVE",
      "createdAt": "2024-01-01T00:00:00Z",
      "lastLoginAt": "2024-01-01T08:00:00Z",
      "assignedPlayersCount": 0
    },
    {
      "id": "uuid",
      "operatorId": "uuid",
      "username": "cajero1_mycasino",
      "role": "CASHIER",
      "status": "ACTIVE",
      "createdAt": "2024-01-01T00:00:00Z",
      "lastLoginAt": "2024-01-01T07:30:00Z",
      "assignedPlayersCount": 2
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 2,
    "pages": 1
  }
}
```

### **POST /api/v1/admin/users**
**Descripci�n**: Crear usuario de backoffice
**Auth**: SUPER_ADMIN (cualquier rol), OPERATOR_ADMIN (solo CASHIER en su operador)

**Request:**
```json
{
  "operatorId": "uuid",
  "username": "cajero3_mycasino",
  "password": "password123",
  "role": "CASHIER",
  "status": "ACTIVE"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "operatorId": "uuid",
  "username": "cajero3_mycasino",
  "role": "CASHIER",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### **GET /api/v1/admin/users/{userId}**
**Descripci�n**: Obtener usuario por ID
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Response 200:**
```json
{
  "id": "uuid",
  "operatorId": "uuid",
  "username": "cajero1_mycasino",
  "role": "CASHIER",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-01T07:30:00Z",
  "assignedPlayers": [
    {
      "playerId": "uuid",
      "playerUsername": "jugador1",
      "assignedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### **PATCH /api/v1/admin/users/{userId}**
**Descripci�n**: Actualizar usuario de backoffice
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Request:**
```json
{
  "status": "SUSPENDED",
  "role": "OPERATOR_ADMIN"
}
```

### **DELETE /api/v1/admin/users/{userId}**
**Descripci�n**: Eliminar usuario de backoffice
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Response 200:**
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

---

## ?? **GESTI�N DE JUGADORES** (Admin)

### **GET /api/v1/admin/players**
**Descripci�n**: Listar jugadores con filtros
**Auth**: SUPER_ADMIN (todos), OPERATOR_ADMIN (de su operador), CASHIER (solo asignados)
**Query Params**:
- `brandId`: uuid
- `status`: ACTIVE|INACTIVE|SUSPENDED|BANNED
- `search`: string (username/email)
- `assignedToCashier`: uuid (filtrar por cajero)
- `hasBalance`: bool
- `page`: int (default: 1)
- `limit`: int (default: 20)

**Response 200:**
```json
{
  "players": [
    {
      "id": "uuid",
      "brandId": "uuid",
      "username": "jugador1",
      "email": "jugador1@mycasino.local",
      "externalId": "ext_001",
      "status": "ACTIVE",
      "balance": 10000,
      "createdAt": "2024-01-01T00:00:00Z",
      "lastLoginAt": "2024-01-01T08:00:00Z",
      "assignedCashiers": [
        {
          "cashierId": "uuid",
          "cashierUsername": "cajero1_mycasino"
        }
      ]
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 4,
    "pages": 1
  }
}
```

### **POST /api/v1/admin/players**
**Descripci�n**: Crear nuevo jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER

**Request:**
```json
{
  "brandId": "uuid",
  "username": "jugador5",
  "email": "jugador5@mycasino.local",
  "externalId": "ext_005",
  "status": "ACTIVE",
  "initialBalance": 15000,
  "password": "demo123"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "brandId": "uuid",
  "username": "jugador5",
  "email": "jugador5@mycasino.local",
  "externalId": "ext_005",
  "status": "ACTIVE",
  "balance": 15000,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### **GET /api/v1/admin/players/{playerId}**
**Descripci�n**: Obtener jugador por ID
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Response 200:**
```json
{
  "id": "uuid",
  "brandId": "uuid",
  "username": "jugador1",
  "email": "jugador1@mycasino.local",
  "externalId": "ext_001",
  "status": "ACTIVE",
  "balance": 10000,
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-01T08:00:00Z",
  "totalBets": 50000,
  "totalWins": 48000,
  "sessionsCount": 15,
  "assignedCashiers": [
    {
      "cashierId": "uuid",
      "cashierUsername": "cajero1_mycasino",
      "assignedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### **PATCH /api/v1/admin/players/{playerId}/status**
**Descripci�n**: Cambiar estado del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Request:**
```json
{
  "status": "SUSPENDED",
  "reason": "Violaci�n de t�rminos y condiciones"
}
```

**Response 200:**
```json
{
  "id": "uuid",
  "status": "SUSPENDED",
  "updatedAt": "2024-01-01T09:00:00Z",
  "reason": "Violaci�n de t�rminos y condiciones"
}
```

---

## ?? **GESTI�N DE BILLETERAS Y TRANSACCIONES** (Admin)

### **GET /api/v1/admin/players/{playerId}/wallet**
**Descripci�n**: Obtener informaci�n de billetera del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Response 200:**
```json
{
  "playerId": "uuid",
  "balance": 10000,
  "currency": "EUR",
  "lastTransactionAt": "2024-01-01T08:30:00Z",
  "recentTransactions": [
    {
      "id": "uuid",
      "delta": -500,
      "reason": "BET",
      "externalRef": "bet_123",
      "createdAt": "2024-01-01T08:30:00Z",
      "meta": {
        "gameCode": "slot_777",
        "sessionId": "sess_456"
      }
    }
  ]
}
```

### **POST /api/v1/admin/players/{playerId}/wallet/adjust**
**Descripci�n**: Ajustar saldo del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Request:**
```json
{
  "amount": 5000,
  "reason": "ADMIN_GRANT",
  "comment": "Bonificaci�n por fidelidad",
  "externalRef": "bonus_001"
}
```

**Response 200:**
```json
{
  "success": true,
  "previousBalance": 10000,
  "newBalance": 15000,
  "delta": 5000,
  "transactionId": "uuid",
  "createdAt": "2024-01-01T09:00:00Z"
}
```

### **GET /api/v1/admin/players/{playerId}/transactions**
**Descripci�n**: Historial de transacciones del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)
**Query Params**:
- `reason`: BET|WIN|ROLLBACK|ADMIN_GRANT|ADMIN_DEDUCT
- `fromDate`: datetime
- `toDate`: datetime
- `page`: int (default: 1)
- `limit`: int (default: 50)

**Response 200:**
```json
{
  "playerId": "uuid",
  "transactions": [
    {
      "id": "uuid",
      "delta": 5000,
      "reason": "ADMIN_GRANT",
      "externalRef": "bonus_001",
      "comment": "Bonificaci�n por fidelidad",
      "createdAt": "2024-01-01T09:00:00Z",
      "balanceAfter": 15000
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 1,
    "pages": 1
  },
  "summary": {
    "totalBets": -50000,
    "totalWins": 48000,
    "totalAdjustments": 5000,
    "netResult": 3000
  }
}
```

---

## ?? **GESTI�N DE ASIGNACIONES CAJERO-JUGADOR** (Admin)

### **POST /api/v1/admin/cashiers/{cashierId}/players/{playerId}**
**Descripci�n**: Asignar jugador a cajero
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Request:**
```json
{
  "assignedAt": "2024-01-01T00:00:00Z"
}
```

**Response 201:**
```json
{
  "cashierId": "uuid",
  "playerId": "uuid",
  "cashierUsername": "cajero1_mycasino",
  "playerUsername": "jugador1",
  "assignedAt": "2024-01-01T00:00:00Z"
}
```

### **GET /api/v1/admin/cashiers/{cashierId}/players**
**Descripci�n**: Listar jugadores asignados a un cajero
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo sus propias asignaciones)

**Response 200:**
```json
{
  "cashierId": "uuid",
  "cashierUsername": "cajero1_mycasino",
  "players": [
    {
      "playerId": "uuid",
      "username": "jugador1",
      "email": "jugador1@mycasino.local",
      "status": "ACTIVE",
      "currentBalance": 10000,
      "assignedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### **DELETE /api/v1/admin/cashiers/{cashierId}/players/{playerId}**
**Descripci�n**: Desasignar jugador de cajero
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Response 200:**
```json
{
  "success": true,
  "message": "Player unassigned successfully"
}
```

### **GET /api/v1/admin/players/{playerId}/cashiers**
**Descripci�n**: Listar cajeros asignados a un jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Response 200:**
```json
{
  "playerId": "uuid",
  "playerUsername": "jugador1",
  "assignedCashiers": [
    {
      "cashierId": "uuid",
      "username": "cajero1_mycasino",
      "role": "CASHIER",
      "assignedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

---

## ?? **GESTI�N DE JUEGOS** (Admin)

### **GET /api/v1/admin/games**
**Descripci�n**: Listar juegos disponibles
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER
**Query Params**:
- `provider`: string
- `enabled`: bool
- `search`: string (nombre/c�digo)
- `page`: int (default: 1)
- `limit`: int (default: 20)

**Response 200:**
```json
{
  "games": [
    {
      "id": "uuid",
      "code": "slot_777",
      "provider": "dummy",
      "name": "Lucky 777 Slot",
      "category": "SLOT",
      "enabled": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "activeBrands": 1
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 4,
    "pages": 1
  }
}
```

### **POST /api/v1/admin/games**
**Descripci�n**: Crear nuevo juego
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Request:**
```json
{
  "code": "new_slot_888",
  "provider": "dummy",
  "name": "Super Lucky 888",
  "category": "SLOT",
  "enabled": true,
  "meta": {
    "rtp": 96.5,
    "volatility": "medium",
    "maxWin": 50000
  }
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "code": "new_slot_888",
  "provider": "dummy",
  "name": "Super Lucky 888",
  "category": "SLOT",
  "enabled": true,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### **GET /api/v1/admin/brands/{brandId}/games**
**Descripci�n**: Listar juegos habilitados para un brand
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Response 200:**
```json
{
  "brandId": "uuid",
  "games": [
    {
      "gameId": "uuid",
      "code": "slot_777",
      "name": "Lucky 777 Slot",
      "provider": "dummy",
      "enabled": true,
      "displayOrder": 1,
      "tags": ["slots", "popular"]
    }
  ]
}
```

### **PUT /api/v1/admin/brands/{brandId}/games/{gameId}**
**Descripci�n**: Configurar juego para brand
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Request:**
```json
{
  "enabled": true,
  "displayOrder": 5,
  "tags": ["slots", "new", "featured"]
}
```

**Response 200:**
```json
{
  "brandId": "uuid",
  "gameId": "uuid",
  "enabled": true,
  "displayOrder": 5,
  "tags": ["slots", "new", "featured"],
  "updatedAt": "2024-01-01T09:00:00Z"
}
```

---

## ?? **ENDPOINTS P�BLICOS** (Cat�logo)

### **GET /api/v1/catalog/games**
**Descripci�n**: Cat�logo p�blico de juegos (resuelto por brand)
**Headers**: `Host: {domain}` (requerido para brand resolution)
**Auth**: No requerida

**Response 200:**
```json
{
  "brand": {
    "code": "mycasino",
    "name": "MiCasino"
  },
  "games": [
    {
      "id": "uuid",
      "code": "slot_777",
      "name": "Lucky 777 Slot",
      "provider": "dummy",
      "category": "SLOT",
      "tags": ["slots", "popular"],
      "displayOrder": 1,
      "launchUrl": "/api/v1/catalog/games/slot_777/launch"
    }
  ]
}
```

### **POST /api/v1/catalog/games/{gameCode}/launch**
**Descripci�n**: Lanzar juego (requiere autenticaci�n de player)
**Headers**: `Host: {domain}`
**Auth**: Player token

**Request:**
```json
{
  "mode": "real",
  "returnUrl": "https://mycasino.local/lobby"
}
```

**Response 200:**
```json
{
  "launchUrl": "https://provider.com/game/slot_777?token=abc123&mode=real",
  "sessionId": "uuid",
  "expiresAt": "2024-01-01T10:00:00Z"
}
```

---

## ?? **GATEWAY API** (Proveedores)

### **POST /api/v1/gateway/balance**
**Descripci�n**: Consultar saldo del jugador
**Headers**: 
- `X-Provider: dummy`
- `X-Signature: HMAC-SHA256`
**Auth**: HMAC validation

**Request:**
```json
{
  "playerId": "ext_001",
  "sessionId": "sess_123",
  "currency": "EUR"
}
```

**Response 200:**
```json
{
  "balance": 10000,
  "currency": "EUR",
  "playerId": "ext_001"
}
```

### **POST /api/v1/gateway/bet**
**Descripci�n**: Procesar apuesta
**Headers**: 
- `X-Provider: dummy`
- `X-Signature: HMAC-SHA256`
**Auth**: HMAC validation

**Request:**
```json
{
  "txId": "bet_123",
  "playerId": "ext_001",
  "sessionId": "sess_123",
  "gameCode": "slot_777",
  "amount": 500,
  "currency": "EUR",
  "roundId": "round_456"
}
```

**Response 200:**
```json
{
  "success": true,
  "balance": 9500,
  "txId": "bet_123",
  "currency": "EUR"
}
```

### **POST /api/v1/gateway/win**
**Descripci�n**: Procesar ganancia
**Headers**: 
- `X-Provider: dummy`
- `X-Signature: HMAC-SHA256`
**Auth**: HMAC validation

**Request:**
```json
{
  "txId": "win_789",
  "playerId": "ext_001",
  "sessionId": "sess_123",
  "gameCode": "slot_777",
  "amount": 1500,
  "currency": "EUR",
  "roundId": "round_456"
}
```

**Response 200:**
```json
{
  "success": true,
  "balance": 11000,
  "txId": "win_789",
  "currency": "EUR"
}
```

### **POST /api/v1/gateway/rollback**
**Descripci�n**: Revertir transacci�n
**Headers**: 
- `X-Provider: dummy`
- `X-Signature: HMAC-SHA256`
**Auth**: HMAC validation

**Request:**
```json
{
  "txId": "rollback_999",
  "originalTxId": "bet_123",
  "playerId": "ext_001",
  "sessionId": "sess_123",
  "amount": 500,
  "currency": "EUR",
  "reason": "GAME_ERROR"
}
```

**Response 200:**
```json
{
  "success": true,
  "balance": 10000,
  "txId": "rollback_999",
  "currency": "EUR"
}
```

---

## ?? **GESTI�N DE PASSWORDS** (Admin)

### **POST /api/v1/admin/users/{userId}/password**
**Descripci�n**: Cambiar password de usuario backoffice
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, o el mismo usuario

**Request:**
```json
{
  "currentPassword": "admin123",
  "newPassword": "nuevaPassword456"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Password updated successfully",
  "lastPasswordChangeAt": "2024-01-01T09:00:00Z"
}
```

### **POST /api/v1/admin/users/{userId}/reset-password**
**Descripci�n**: Reset password (solo SUPER_ADMIN y OPERATOR_ADMIN)
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Request:**
```json
{
  "newPassword": "resetPassword789",
  "forceChangeOnNextLogin": true
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Password reset successfully",
  "temporaryPassword": false
}
```

### **POST /api/v1/admin/players/{playerId}/password**
**Descripci�n**: Cambiar password de jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Request:**
```json
{
  "newPassword": "nuevaPasswordJugador"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Player password updated successfully"
}
```

---

## ?? **ENDPOINTS DE SETUP Y UTILIDADES** (Admin)

### **POST /api/v1/admin/setup/demo-site**
**Descripci�n**: Crear sitio completo de demo con un solo endpoint
**Auth**: SUPER_ADMIN

**Request:**
```json
{
  "operatorName": "MiCasino Corp",
  "brandCode": "mycasino",
  "brandName": "MiCasino",
  "domain": "mycasino.local",
  "adminDomain": "admin.mycasino.local",
  "corsOrigins": ["http://localhost:3000"],
  "locale": "es-ES",
  "adminCredentials": {
    "username": "admin_mycasino",
    "password": "admin123"
  },
  "cashiers": [
    {"username": "cajero1_mycasino", "password": "admin123"},
    {"username": "cajero2_mycasino", "password": "admin123"}
  ],
  "players": [
    {"username": "jugador1", "password": "demo", "email": "jugador1@mycasino.local", "initialBalance": 10000},
    {"username": "jugador2", "password": "demo", "email": "jugador2@mycasino.local", "initialBalance": 10000}
  ],
  "assignCashiersToPlayers": {
    "cajero1_mycasino": ["jugador1"],
    "cajero2_mycasino": ["jugador2"]
  },
  "includeGames": true,
  "includeProviderConfig": {
    "provider": "dummy",
    "secret": "mi_secreto_hmac_super_seguro_32_chars"
  }
}
```

**Response 201:**
```json
{
  "success": true,
  "message": "Demo site created successfully",
  "operator": {
    "id": "uuid",
    "name": "MiCasino Corp"
  },
  "brand": {
    "id": "uuid",
    "code": "mycasino",
    "domain": "mycasino.local",
    "adminDomain": "admin.mycasino.local"
  },
  "users": {
    "admin": {"id": "uuid", "username": "admin_mycasino"},
    "cashiers": [
      {"id": "uuid", "username": "cajero1_mycasino"},
      {"id": "uuid", "username": "cajero2_mycasino"}
    ]
  },
  "players": [
    {"id": "uuid", "username": "jugador1", "balance": 10000},
    {"id": "uuid", "username": "jugador2", "balance": 10000}
  ],
  "assignments": {
    "cajero1_mycasino": ["jugador1"],
    "cajero2_mycasino": ["jugador2"]
  },
  "games": {
    "total": 4,
    "enabled": 4
  },
  "provider": {
    "code": "dummy",
    "configured": true
  }
}
```

### **GET /api/v1/admin/setup/validate**
**Descripci�n**: Validar que un sitio est� completamente configurado
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Query Params**:
- `brandCode`: string
- `domain`: string

**Response 200:**
```json
{
  "valid": true,
  "brand": {
    "configured": true,
    "domain": "mycasino.local",
    "adminDomain": "admin.mycasino.local",
    "status": "ACTIVE"
  },
  "users": {
    "adminCount": 1,
    "cashierCount": 2,
    "totalUsers": 3
  },
  "players": {
    "totalCount": 2,
    "activeCount": 2,
    "totalBalance": 20000
  },
  "assignments": {
    "totalAssignments": 2,
    "cashiersWithPlayers": 2
  },
  "games": {
    "totalGames": 4,
    "enabledGames": 4
  },
  "providers": {
    "configuredProviders": ["dummy"]
  },
  "missing": []
}
```

---

## ?? **C�DIGOS DE RESPUESTA HTTP**

### **�xito**
- `200 OK`: Operaci�n exitosa
- `201 Created`: Recurso creado exitosamente
- `204 No Content`: Operaci�n exitosa sin contenido

### **Errores del Cliente**
- `400 Bad Request`: Datos de entrada inv�lidos
- `401 Unauthorized`: No autenticado o token inv�lido
- `403 Forbidden`: Sin permisos para la operaci�n
- `404 Not Found`: Recurso no encontrado
- `409 Conflict`: Conflicto (ej: username duplicado, saldo insuficiente)
- `422 Unprocessable Entity`: Errores de validaci�n

### **Errores del Servidor**
- `500 Internal Server Error`: Error interno del servidor
- `503 Service Unavailable`: Servicio temporalmente no disponible

---

## ?? **Configuraci�n de Desarrollo**

### **Headers Requeridos**
- **Brand Resolution**: `Host: {domain}` para todos los endpoints
- **Admin Endpoints**: `Host: admin.{domain}`
- **Player Endpoints**: `Host: {domain}`

### **Autenticaci�n**
- **Bearer Token**: `Authorization: Bearer {jwt_token}`
- **Cookies**: Autom�ticas en navegadores
  - Backoffice: `bk.token` (Path: /admin)
  - Players: `pl.token` (Path: /)

### **CORS**
- Configurado din�micamente por brand
- `corsOrigins` en configuraci�n del brand
- Credentials habilitadas para cookies

### **Variables de Entorno**
```bash
# appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=casino_platform;Username=postgres;Password=password"
  },
  "Auth": {
    "Issuer": "casino",
    "JwtKey": "REEMPLAZAR_POR_CLAVE_DE_32_O_MAS_CARACTERES_EN_PRODUCCION"
  }
}
```

---

## ?? **Notas Importantes**

1. **Separaci�n de Contextos**: Los tokens de backoffice y players NO son intercambiables
2. **Brand Context**: Todos los endpoints p�blicos requieren resoluci�n de brand por host
3. **Scoping de Datos**: Los usuarios ven solo datos de su operador/brand
4. **Idempotencia**: Las transacciones del gateway usan `externalRef` para evitar duplicados
5. **HMAC Security**: Los endpoints del gateway validan firmas HMAC del proveedor
6. **Audit Trail**: Todas las operaciones admin se registran para auditor�a
7. **Balance Handling**: Los balances se manejan en centavos (bigint) para precisi�n
8. **Cookie Security**: HttpOnly, Secure, SameSite configurado para m�xima seguridad

---

## ?? **Estado de Implementaci�n**

### ? **Completamente Implementado**
- Autenticaci�n h�brida JWT + Cookies
- Gesti�n de operadores y brands
- Gesti�n de usuarios backoffice
- Gesti�n de jugadores
- Sistema de billeteras y transacciones
- Gateway API con HMAC
- Cat�logo p�blico de juegos
- Gesti�n de asignaciones cajero-jugador
- Gesti�n de passwords

### ?? **En Desarrollo**
- Endpoints de setup automatizado
- Sistema de reportes y analytics
- Gesti�n de promociones y bonos
- Sistema de sesiones de juego
- API de auditor�a completa

### ?? **Pendiente de Implementaci�n**
- Integraci�n con proveedores reales
- Sistema de pagos
- KYC y verificaci�n de identidad
- Sistema de notificaciones
- Interfaz de administraci�n web

Esta documentaci�n cubre todos los endpoints actualmente implementados en la plataforma de casino. La API est� dise�ada para ser escalable, segura y compatible con m�ltiples brands y operadores.