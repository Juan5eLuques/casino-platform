# Casino Platform API - Documentación Completa de Roles y CRUD

## Descripción General
Esta API permite la gestión completa de usuarios y entidades en la plataforma de casino. Soporta 4 tipos de roles principales con diferentes niveles de acceso y permisos.

## Autenticación
- **Esquema**: JWT Bearer Token
- **Header**: `Authorization: Bearer <token>`
- **Cookie alternativa**: `bk.token` (para backoffice)

## Roles del Sistema

### 1. 🔥 SUPER_ADMIN
- **Descripción**: Acceso total a todo el sistema
- **Permisos**: CRUD completo en todas las entidades
- **Scope**: Sin restricciones

### 2. 👨‍💼 OPERATOR_ADMIN  
- **Descripción**: Administrador limitado por operador
- **Permisos**: CRUD en entidades de su operador únicamente
- **Scope**: Limitado por `operator_id`

### 3. 🏦 CASHIER
- **Descripción**: Cajero con acceso limitado a jugadores asignados
- **Permisos**: Solo lectura/modificación de jugadores asignados
- **Scope**: Solo jugadores en relación `cashier_player`

### 4. 🎮 JUGADORES
- **Descripción**: Gestionados por roles administrativos
- **Permisos**: Sin acceso directo al backoffice
- **Scope**: Gestionados a través de APIs de administración

---

# Endpoints por Entidad

## 🏢 OPERADORES

### Crear Operador
```http
POST /api/v1/admin/operators
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "Casino Operator Inc",
  "contactEmail": "admin@operator.com",
  "status": "ACTIVE"
}
```

**Respuesta (201)**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Casino Operator Inc",
  "contactEmail": "admin@operator.com",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Permisos**: Solo `SUPER_ADMIN`

### Listar Operadores
```http
GET /api/v1/admin/operators?page=1&pageSize=10&status=ACTIVE
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "data": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Casino Operator Inc",
      "contactEmail": "admin@operator.com",
      "status": "ACTIVE",
      "brandsCount": 5,
      "usersCount": 12
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

**Permisos**: 
- `SUPER_ADMIN`: Ve todos los operadores
- `OPERATOR_ADMIN`: Solo su propio operador

### Obtener Operador
```http
GET /api/v1/admin/operators/{operatorId}
Authorization: Bearer <token>
```

### Actualizar Operador
```http
PATCH /api/v1/admin/operators/{operatorId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "Updated Operator Name",
  "contactEmail": "new-admin@operator.com"
}
```

### Eliminar Operador
```http
DELETE /api/v1/admin/operators/{operatorId}
Authorization: Bearer <token>
```

**Permisos**: Solo `SUPER_ADMIN`

---

## 👥 USUARIOS DE BACKOFFICE

### Crear Usuario de Backoffice
```http
POST /api/v1/admin/users
Content-Type: application/json
Authorization: Bearer <token>

{
  "username": "admin.user",
  "password": "SecurePass123!",
  "email": "admin@example.com",
  "role": "OPERATOR_ADMIN",
  "operatorId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "ACTIVE"
}
```

**Respuesta (201)**:
```json
{
  "id": "456e7890-e89b-12d3-a456-426614174001",
  "username": "admin.user",
  "email": "admin@example.com",
  "role": "OPERATOR_ADMIN",
  "operatorId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "ACTIVE",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Roles Disponibles**:
- `SUPER_ADMIN`: Solo puede ser creado por otro SUPER_ADMIN
- `OPERATOR_ADMIN`: Puede ser creado por SUPER_ADMIN o OPERATOR_ADMIN del mismo operador
- `CASHIER`: Puede ser creado por SUPER_ADMIN o OPERATOR_ADMIN

**Permisos**: 
- `SUPER_ADMIN`: Puede crear cualquier rol
- `OPERATOR_ADMIN`: Puede crear OPERATOR_ADMIN y CASHIER en su operador
- `CASHIER`: No puede crear usuarios

### Listar Usuarios de Backoffice
```http
GET /api/v1/admin/users?page=1&pageSize=10&role=CASHIER&status=ACTIVE
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "data": [
    {
      "id": "456e7890-e89b-12d3-a456-426614174001",
      "username": "cashier.user",
      "email": "cashier@example.com",
      "role": "CASHIER",
      "operatorName": "Casino Operator Inc",
      "status": "ACTIVE",
      "lastLoginAt": "2024-01-01T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

### Obtener Usuario de Backoffice
```http
GET /api/v1/admin/users/{userId}
Authorization: Bearer <token>
```

### Actualizar Usuario de Backoffice
```http
PATCH /api/v1/admin/users/{userId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "email": "updated@example.com",
  "status": "INACTIVE"
}
```

### Eliminar Usuario de Backoffice
```http
DELETE /api/v1/admin/users/{userId}
Authorization: Bearer <token>
```

### Cambiar Estado de Usuario
```http
PATCH /api/v1/admin/users/{userId}/status
Content-Type: application/json
Authorization: Bearer <token>

{
  "status": "INACTIVE"
}
```

---

## 🏷️ BRANDS (SITIOS)

### Crear Brand
```http
POST /api/v1/admin/brands
Content-Type: application/json
Authorization: Bearer <token>

{
  "code": "BET30",
  "name": "Bet30 Casino",
  "locale": "es-ES",
  "domain": "bet30.com",
  "adminDomain": "admin.bet30.com",
  "corsOrigins": ["https://bet30.com", "https://admin.bet30.com"],
  "theme": {
    "primaryColor": "#1a365d",
    "logo": "https://cdn.bet30.com/logo.png"
  },
  "settings": {
    "maxBetAmount": 1000,
    "currency": "EUR",
    "timezone": "Europe/Madrid"
  }
}
```

**Respuesta (201)**:
```json
{
  "id": "789e0123-e89b-12d3-a456-426614174002",
  "code": "BET30",
  "name": "Bet30 Casino",
  "locale": "es-ES",
  "domain": "bet30.com",
  "adminDomain": "admin.bet30.com",
  "corsOrigins": ["https://bet30.com", "https://admin.bet30.com"],
  "status": "ACTIVE",
  "operatorId": "123e4567-e89b-12d3-a456-426614174000",
  "createdAt": "2024-01-01T00:00:00Z",
  "theme": { "primaryColor": "#1a365d", "logo": "https://cdn.bet30.com/logo.png" },
  "settings": { "maxBetAmount": 1000, "currency": "EUR", "timezone": "Europe/Madrid" }
}
```

### Listar Brands
```http
GET /api/v1/admin/brands?page=1&pageSize=10&status=ACTIVE&operatorId={operatorId}
Authorization: Bearer <token>
```

### Obtener Brand por ID
```http
GET /api/v1/admin/brands/{brandId}
Authorization: Bearer <token>
```

### Obtener Brand por Host
```http
GET /api/v1/admin/brands/by-host/bet30.com
Authorization: Bearer <token>
```

### Actualizar Brand
```http
PATCH /api/v1/admin/brands/{brandId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "Updated Casino Name",
  "corsOrigins": ["https://bet30.com", "https://admin.bet30.com", "https://m.bet30.com"]
}
```

### Eliminar Brand
```http
DELETE /api/v1/admin/brands/{brandId}
Authorization: Bearer <token>
```

### Cambiar Estado de Brand
```http
POST /api/v1/admin/brands/{brandId}/status
Content-Type: application/json
Authorization: Bearer <token>

{
  "status": "INACTIVE"
}
```

### Gestión de Settings de Brand

#### Obtener Settings
```http
GET /api/v1/admin/brands/{brandId}/settings
Authorization: Bearer <token>
```

#### Reemplazar Settings Completas
```http
PUT /api/v1/admin/brands/{brandId}/settings
Content-Type: application/json
Authorization: Bearer <token>

{
  "maxBetAmount": 2000,
  "currency": "USD",
  "timezone": "America/New_York",
  "features": {
    "liveChat": true,
    "bonuses": true
  }
}
```

#### Actualizar Settings Parciales
```http
PATCH /api/v1/admin/brands/{brandId}/settings
Content-Type: application/json
Authorization: Bearer <token>

{
  "maxBetAmount": 1500,
  "features.liveChat": false
}
```

### Gestión de Proveedores por Brand

#### Obtener Proveedores del Brand
```http
GET /api/v1/admin/brands/{brandId}/providers
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "brandId": "789e0123-e89b-12d3-a456-426614174002",
  "providers": [
    {
      "providerCode": "pragmatic",
      "secret": "encrypted_secret_key",
      "allowNegativeOnRollback": false,
      "isEnabled": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

#### Configurar Proveedor
```http
PUT /api/v1/admin/brands/{brandId}/providers/{providerCode}
Content-Type: application/json
Authorization: Bearer <token>

{
  "allowNegativeOnRollback": true,
  "isEnabled": true,
  "meta": {
    "customConfig": "value"
  }
}
```

#### Rotar Secreto de Proveedor
```http
POST /api/v1/admin/brands/{brandId}/providers/{providerCode}/rotate-secret
Content-Type: application/json
Authorization: Bearer <token>

{
  "reason": "Security rotation"
}
```

### Catálogo de Juegos por Brand
```http
GET /api/v1/admin/brands/{brandId}/catalog
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
[
  {
    "gameId": "abc123",
    "code": "sweet-bonanza",
    "name": "Sweet Bonanza",
    "provider": "pragmatic",
    "enabled": true,
    "displayOrder": 1,
    "tags": ["slots", "featured"]
  }
]
```

---

## 🎮 JUGADORES

### Crear Jugador
```http
POST /api/v1/admin/players
Content-Type: application/json
Authorization: Bearer <token>

{
  "username": "player123",
  "password": "PlayerPass123!",
  "email": "player@example.com",
  "brandId": "789e0123-e89b-12d3-a456-426614174002",
  "initialBalance": 100.00,
  "currency": "EUR"
}
```

**Respuesta (201)**:
```json
{
  "id": "901e2345-e89b-12d3-a456-426614174003",
  "username": "player123",
  "email": "player@example.com",
  "brandCode": "BET30",
  "status": "ACTIVE",
  "balance": 100.00,
  "currency": "EUR",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Permisos**:
- `SUPER_ADMIN`: Puede crear en cualquier brand
- `OPERATOR_ADMIN`: Solo en brands de su operador
- `CASHIER`: No puede crear jugadores

### Listar Jugadores
```http
GET /api/v1/admin/players?page=1&pageSize=10&status=ACTIVE&brandId={brandId}
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "data": [
    {
      "id": "901e2345-e89b-12d3-a456-426614174003",
      "username": "player123",
      "email": "player@example.com",
      "brandCode": "BET30",
      "status": "ACTIVE",
      "balance": 150.50,
      "currency": "EUR",
      "lastLoginAt": "2024-01-01T15:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

**Permisos**:
- `SUPER_ADMIN`: Ve todos los jugadores
- `OPERATOR_ADMIN`: Solo jugadores de sus brands
- `CASHIER`: Solo jugadores asignados

### Obtener Jugador
```http
GET /api/v1/admin/players/{playerId}
Authorization: Bearer <token>
```

### Actualizar Jugador
```http
PATCH /api/v1/admin/players/{playerId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "email": "newemail@example.com",
  "status": "ACTIVE"
}
```

### Cambiar Estado de Jugador
```http
PATCH /api/v1/admin/players/{playerId}/status
Content-Type: application/json
Authorization: Bearer <token>

{
  "status": "BLOCKED"
}
```

### Ajustar Wallet de Jugador
```http
POST /api/v1/admin/players/{playerId}/wallet/adjust
Content-Type: application/json
Authorization: Bearer <token>

{
  "amount": 50.00,
  "reason": "BONUS",
  "description": "Welcome bonus",
  "externalRef": "bonus_2024_001"
}
```

**Respuesta (200)**:
```json
{
  "success": true,
  "transactionId": "tx_789012",
  "previousBalance": 100.00,
  "newBalance": 150.00,
  "amount": 50.00,
  "reason": "BONUS",
  "processedAt": "2024-01-01T16:00:00Z"
}
```

**Tipos de Ajuste**:
- `amount > 0`: Crédito (depósito)
- `amount < 0`: Débito (retiro) - requiere saldo suficiente

---

## 🏦 GESTIÓN CASHIER-PLAYER

### Asignar Jugador a Cajero
```http
POST /api/v1/admin/cashiers/{cashierId}/players/{playerId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "assignedBy": "OPERATOR_ADMIN",
  "notes": "Assigned for VIP support"
}
```

**Respuesta (201)**:
```json
{
  "cashierId": "456e7890-e89b-12d3-a456-426614174001",
  "playerId": "901e2345-e89b-12d3-a456-426614174003",
  "assignedAt": "2024-01-01T17:00:00Z",
  "assignedBy": "234e5678-e89b-12d3-a456-426614174004",
  "notes": "Assigned for VIP support"
}
```

**Permisos**: Solo `SUPER_ADMIN` y `OPERATOR_ADMIN`

### Obtener Jugadores de un Cajero
```http
GET /api/v1/admin/cashiers/{cashierId}/players
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "cashierId": "456e7890-e89b-12d3-a456-426614174001",
  "cashierUsername": "cashier.user",
  "players": [
    {
      "id": "901e2345-e89b-12d3-a456-426614174003",
      "username": "player123",
      "email": "player@example.com",
      "brandCode": "BET30",
      "balance": 150.50,
      "status": "ACTIVE",
      "assignedAt": "2024-01-01T17:00:00Z"
    }
  ]
}
```

### Desasignar Jugador de Cajero
```http
DELETE /api/v1/admin/cashiers/{cashierId}/players/{playerId}
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "success": true,
  "message": "Player unassigned successfully"
}
```

### Obtener Cajeros de un Jugador
```http
GET /api/v1/admin/players/{playerId}/cashiers
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "playerId": "901e2345-e89b-12d3-a456-426614174003",
  "playerUsername": "player123",
  "cashiers": [
    {
      "id": "456e7890-e89b-12d3-a456-426614174001",
      "username": "cashier.user",
      "email": "cashier@example.com",
      "assignedAt": "2024-01-01T17:00:00Z"
    }
  ]
}
```

---

## 🎯 GESTIÓN DE JUEGOS POR BRAND

### Asignar Juego a Brand
```http
POST /api/v1/admin/brands/{brandId}/games
Content-Type: application/json
Authorization: Bearer <token>

{
  "gameId": "abc123",
  "enabled": true,
  "displayOrder": 1,
  "tags": ["slots", "featured"]
}
```

### Obtener Juegos de un Brand
```http
GET /api/v1/admin/brands/{brandId}/games
Authorization: Bearer <token>
```

### Actualizar Configuración de Juego en Brand
```http
PATCH /api/v1/admin/brands/{brandId}/games/{gameId}
Content-Type: application/json
Authorization: Bearer <token>

{
  "enabled": false,
  "displayOrder": 5,
  "tags": ["slots"]
}
```

### Remover Juego de Brand
```http
DELETE /api/v1/admin/brands/{brandId}/games/{gameId}
Authorization: Bearer <token>
```

---

## 📊 AUDITORÍA

### Auditoría de Backoffice
```http
GET /api/v1/admin/audit/backoffice?userId={userId}&action=USER_CREATED&page=1&pageSize=50
Authorization: Bearer <token>
```

**Respuesta (200)**:
```json
{
  "data": [
    {
      "id": "audit_001",
      "action": "USER_CREATED",
      "targetType": "BackofficeUser",
      "targetId": "456e7890-e89b-12d3-a456-426614174001",
      "meta": {
        "username": "new.user",
        "role": "CASHIER"
      },
      "createdAt": "2024-01-01T18:00:00Z",
      "user": {
        "username": "admin.user",
        "role": "OPERATOR_ADMIN"
      }
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 1,
  "totalPages": 1
}
```

### Auditoría de Proveedores
```http
GET /api/v1/admin/audit/provider?provider=pragmatic&action=DEBIT&sessionId=session_123
Authorization: Bearer <token>
```

---

## 🔐 MATRIZ DE PERMISOS

| Endpoint | SUPER_ADMIN | OPERATOR_ADMIN | CASHIER |
|----------|-------------|----------------|---------|
| **OPERADORES** |
| POST /operators | ✅ | ❌ | ❌ |
| GET /operators | ✅ (todos) | ✅ (propio) | ❌ |
| PATCH /operators/{id} | ✅ | ✅ (propio) | ❌ |
| DELETE /operators/{id} | ✅ | ❌ | ❌ |
| **USUARIOS BACKOFFICE** |
| POST /users | ✅ | ✅ (scope) | ❌ |
| GET /users | ✅ (todos) | ✅ (scope) | ❌ |
| PATCH /users/{id} | ✅ | ✅ (scope) | ❌ |
| DELETE /users/{id} | ✅ | ✅ (scope) | ❌ |
| **BRANDS** |
| POST /brands | ✅ | ✅ (scope) | ❌ |
| GET /brands | ✅ (todos) | ✅ (scope) | ❌ |
| PATCH /brands/{id} | ✅ | ✅ (scope) | ❌ |
| DELETE /brands/{id} | ✅ | ✅ (scope) | ❌ |
| **JUGADORES** |
| POST /players | ✅ | ✅ (scope) | ❌ |
| GET /players | ✅ (todos) | ✅ (scope) | ✅ (asignados) |
| PATCH /players/{id} | ✅ | ✅ (scope) | ✅ (asignados) |
| POST /players/{id}/wallet/adjust | ✅ | ✅ (scope) | ✅ (asignados) |
| **CASHIER-PLAYER** |
| POST /cashiers/{id}/players/{id} | ✅ | ✅ | ❌ |
| DELETE /cashiers/{id}/players/{id} | ✅ | ✅ | ❌ |

---

## 🚨 CÓDIGOS DE ERROR

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| 400 | Bad Request | Datos de entrada inválidos |
| 401 | Unauthorized | Token JWT faltante o inválido |
| 403 | Forbidden | Sin permisos para la operación |
| 404 | Not Found | Entidad no encontrada o sin acceso |
| 409 | Conflict | Violación de unicidad (username, email, domain) |
| 422 | Validation Error | Errores de validación de campos |
| 500 | Internal Server Error | Error interno del servidor |

---

## 📝 NOTAS IMPORTANTES

### Scoping y Seguridad
- Todos los endpoints validan automáticamente el scope según el rol del usuario
- Los `OPERATOR_ADMIN` solo pueden ver/modificar entidades de su operador
- Los `CASHIER` solo pueden gestionar jugadores que tienen asignados
- Las validaciones de permisos se aplican tanto en lectura como en escritura

### Paginación
- Parámetros estándar: `page` (1-based), `pageSize` (default: 10, max: 100)
- Respuesta incluye: `page`, `pageSize`, `totalCount`, `totalPages`

### Filtros Comunes
- `status`: ACTIVE, INACTIVE (para usuarios y brands)
- `role`: SUPER_ADMIN, OPERATOR_ADMIN, CASHIER (para usuarios)
- `operatorId`: UUID del operador (scope automático para OPERATOR_ADMIN)
- `brandId`: UUID del brand (scope automático cuando aplica)

### Auditoría
- Todas las operaciones de escritura se auditan automáticamente
- Los logs incluyen: usuario que ejecuta, acción, entidad afectada, metadatos
- La auditoría respeta el mismo scoping que los permisos del usuario

### Idempotencia
- Los ajustes de wallet soportan `externalRef` para evitar duplicados
- Las operaciones de creación validan unicidad en campos clave

### Rate Limiting
- Límites por endpoint según el tipo de operación
- Headers de respuesta incluyen información de límites restantes

Esta documentación cubre todos los endpoints CRUD disponibles para los 4 roles del sistema. Cada endpoint incluye ejemplos de request/response y la matriz de permisos correspondiente.