# Casino Platform API - Documentación Completa de Endpoints

## ?? **Resumen General**

Esta es una plataforma de casino multi-brand con autenticación híbrida JWT+Cookies, separación de contextos (Backoffice vs Players), y sistema de gestión completo.

### **Arquitectura de Autenticación**
- **Backoffice**: JWT audience "backoffice", Cookie "bk.token" (Path: /admin)
- **Players**: JWT audience "player", Cookie "pl.token" (Path: /)
- **Roles**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (backoffice) / PLAYER (site)

### **Resolución de Brand**
- **Por Host**: BrandResolverMiddleware identifica el brand por dominio
- **Context Injection**: Automático en todos los endpoints basado en Host header

---

## ?? **AUTENTICACIÓN Y AUTORIZACIÓN**

### **Backoffice Authentication**

#### **POST /api/v1/admin/auth/login**
**Descripción**: Login para usuarios de backoffice
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
**Descripción**: Obtener perfil del usuario actual
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
**Descripción**: Cerrar sesión de backoffice
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
**Descripción**: Login para jugadores
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
**Descripción**: Obtener perfil del jugador actual
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
**Descripción**: Cerrar sesión de jugador
**Auth**: Bearer token o cookie pl.token

**Response 200:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

## ?? **GESTIÓN DE OPERADORES** (Admin)

### **GET /api/v1/admin/operators**
**Descripción**: Listar operadores con filtros y paginación
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
**Descripción**: Crear nuevo operador
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
**Descripción**: Actualizar operador
**Auth**: SUPER_ADMIN

**Request:**
```json
{
  "name": "Casino Corp Actualizado",
  "status": "INACTIVE"
}
```

---

## ?? **GESTIÓN DE BRANDS** (Admin)

### **GET /api/v1/admin/brands**
**Descripción**: Listar brands con filtros
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
**Descripción**: Crear nuevo brand
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
**Descripción**: Actualizar brand
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
**Descripción**: Configurar proveedor para brand
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

## ?? **GESTIÓN DE USUARIOS BACKOFFICE** (Admin)

### **GET /api/v1/admin/users**
**Descripción**: Listar usuarios de backoffice
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
**Descripción**: Crear usuario de backoffice
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
**Descripción**: Obtener usuario por ID
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
**Descripción**: Actualizar usuario de backoffice
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Request:**
```json
{
  "status": "SUSPENDED",
  "role": "OPERATOR_ADMIN"
}
```

### **DELETE /api/v1/admin/users/{userId}**
**Descripción**: Eliminar usuario de backoffice
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN (solo de su operador)

**Response 200:**
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

---

## ?? **GESTIÓN DE JUGADORES** (Admin)

### **GET /api/v1/admin/players**
**Descripción**: Listar jugadores con filtros
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
**Descripción**: Crear nuevo jugador
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
**Descripción**: Obtener jugador por ID
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
**Descripción**: Cambiar estado del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Request:**
```json
{
  "status": "SUSPENDED",
  "reason": "Violación de términos y condiciones"
}
```

**Response 200:**
```json
{
  "id": "uuid",
  "status": "SUSPENDED",
  "updatedAt": "2024-01-01T09:00:00Z",
  "reason": "Violación de términos y condiciones"
}
```

---

## ?? **GESTIÓN DE BILLETERAS Y TRANSACCIONES** (Admin)

### **GET /api/v1/admin/players/{playerId}/wallet**
**Descripción**: Obtener información de billetera del jugador
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
**Descripción**: Ajustar saldo del jugador
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (solo asignados)

**Request:**
```json
{
  "amount": 5000,
  "reason": "ADMIN_GRANT",
  "comment": "Bonificación por fidelidad",
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
**Descripción**: Historial de transacciones del jugador
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
      "comment": "Bonificación por fidelidad",
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

## ?? **GESTIÓN DE ASIGNACIONES CAJERO-JUGADOR** (Admin)

### **POST /api/v1/admin/cashiers/{cashierId}/players/{playerId}**
**Descripción**: Asignar jugador a cajero
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
**Descripción**: Listar jugadores asignados a un cajero
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
**Descripción**: Desasignar jugador de cajero
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN

**Response 200:**
```json
{
  "success": true,
  "message": "Player unassigned successfully"
}
```

### **GET /api/v1/admin/players/{playerId}/cashiers**
**Descripción**: Listar cajeros asignados a un jugador
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

## ?? **GESTIÓN DE JUEGOS** (Admin)

### **GET /api/v1/admin/games**
**Descripción**: Listar juegos disponibles
**Auth**: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER
**Query Params**:
- `provider`: string
- `enabled`: bool
- `search`: string (nombre/código)
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
**Descripción**: Crear nuevo juego
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
**Descripción**: Listar juegos habilitados para un brand
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
**Descripción**: Configurar juego para brand
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

## ?? **ENDPOINTS PÚBLICOS** (Catálogo)

### **GET /api/v1/catalog/games**
**Descripción**: Catálogo público de juegos (resuelto por brand)
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
**Descripción**: Lanzar juego (requiere autenticación de player)
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
**Descripción**: Consultar saldo del jugador
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
**Descripción**: Procesar apuesta
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
**Descripción**: Procesar ganancia
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
**Descripción**: Revertir transacción
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

## ?? **GESTIÓN DE PASSWORDS** (Admin)

### **POST /api/v1/admin/users/{userId}/password**
**Descripción**: Cambiar password de usuario backoffice
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
**Descripción**: Reset password (solo SUPER_ADMIN y OPERATOR_ADMIN)
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
**Descripción**: Cambiar password de jugador
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
**Descripción**: Crear sitio completo de demo con un solo endpoint
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
**Descripción**: Validar que un sitio esté completamente configurado
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

## ?? **CÓDIGOS DE RESPUESTA HTTP**

### **Éxito**
- `200 OK`: Operación exitosa
- `201 Created`: Recurso creado exitosamente
- `204 No Content`: Operación exitosa sin contenido

### **Errores del Cliente**
- `400 Bad Request`: Datos de entrada inválidos
- `401 Unauthorized`: No autenticado o token inválido
- `403 Forbidden`: Sin permisos para la operación
- `404 Not Found`: Recurso no encontrado
- `409 Conflict`: Conflicto (ej: username duplicado, saldo insuficiente)
- `422 Unprocessable Entity`: Errores de validación

### **Errores del Servidor**
- `500 Internal Server Error`: Error interno del servidor
- `503 Service Unavailable`: Servicio temporalmente no disponible

---

## ?? **Configuración de Desarrollo**

### **Headers Requeridos**
- **Brand Resolution**: `Host: {domain}` para todos los endpoints
- **Admin Endpoints**: `Host: admin.{domain}`
- **Player Endpoints**: `Host: {domain}`

### **Autenticación**
- **Bearer Token**: `Authorization: Bearer {jwt_token}`
- **Cookies**: Automáticas en navegadores
  - Backoffice: `bk.token` (Path: /admin)
  - Players: `pl.token` (Path: /)

### **CORS**
- Configurado dinámicamente por brand
- `corsOrigins` en configuración del brand
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

1. **Separación de Contextos**: Los tokens de backoffice y players NO son intercambiables
2. **Brand Context**: Todos los endpoints públicos requieren resolución de brand por host
3. **Scoping de Datos**: Los usuarios ven solo datos de su operador/brand
4. **Idempotencia**: Las transacciones del gateway usan `externalRef` para evitar duplicados
5. **HMAC Security**: Los endpoints del gateway validan firmas HMAC del proveedor
6. **Audit Trail**: Todas las operaciones admin se registran para auditoría
7. **Balance Handling**: Los balances se manejan en centavos (bigint) para precisión
8. **Cookie Security**: HttpOnly, Secure, SameSite configurado para máxima seguridad

---

## ?? **Estado de Implementación**

### ? **Completamente Implementado**
- Autenticación híbrida JWT + Cookies
- Gestión de operadores y brands
- Gestión de usuarios backoffice
- Gestión de jugadores
- Sistema de billeteras y transacciones
- Gateway API con HMAC
- Catálogo público de juegos
- Gestión de asignaciones cajero-jugador
- Gestión de passwords

### ?? **En Desarrollo**
- Endpoints de setup automatizado
- Sistema de reportes y analytics
- Gestión de promociones y bonos
- Sistema de sesiones de juego
- API de auditoría completa

### ?? **Pendiente de Implementación**
- Integración con proveedores reales
- Sistema de pagos
- KYC y verificación de identidad
- Sistema de notificaciones
- Interfaz de administración web

Esta documentación cubre todos los endpoints actualmente implementados en la plataforma de casino. La API está diseñada para ser escalable, segura y compatible con múltiples brands y operadores.