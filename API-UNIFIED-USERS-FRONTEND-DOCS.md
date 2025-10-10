# ?? API Documentation - Sistema de Usuarios Unificado

## ?? **Endpoint Base: `/api/v1/admin/users`**

Todos los tipos de usuarios se manejan a través de este único endpoint:
- ? **Usuarios de Backoffice**: SUPER_ADMIN, BRAND_ADMIN, CASHIER
- ? **Jugadores**: PLAYER

---

## ?? **1. CREAR USUARIO**

### `POST /api/v1/admin/users`

Crea cualquier tipo de usuario. El tipo se determina automáticamente:
- **Si `role` tiene valor** ? Usuario de backoffice
- **Si `role` es `null`** ? Jugador

#### **Request Body:**

```typescript
interface CreateUserRequest {
  username: string;           // Requerido: 3-100 caracteres
  password: string;           // Requerido: mínimo 4 caracteres (modo dev)
  
  // Para usuarios de backoffice (si se incluye role)
  role?: 'SUPER_ADMIN' | 'BRAND_ADMIN' | 'CASHIER';
  commissionRate?: number;    // 0-100, solo para CASHIER
  parentCashierId?: string;   // Solo para CASHIER subordinados
  
  // Para jugadores (si role es null/undefined)
  playerStatus?: 'ACTIVE' | 'INACTIVE' | 'SUSPENDED';
  email?: string;             // Email válido
  externalId?: string;        // ID externo del sistema
  initialBalance?: number;    // Balance inicial (default: 0)
}
```

#### **Ejemplos:**

**Crear SUPER_ADMIN:**
```json
{
  "username": "superadmin1",
  "password": "1234",
  "role": "SUPER_ADMIN"
}
```

**Crear BRAND_ADMIN:**
```json
{
  "username": "brandadmin1", 
  "password": "1234",
  "role": "BRAND_ADMIN"
}
```

**Crear CASHIER:**
```json
{
  "username": "cajero1",
  "password": "1234", 
  "role": "CASHIER",
  "commissionRate": 15
}
```

**Crear CASHIER subordinado:**
```json
{
  "username": "subcajero1",
  "password": "1234",
  "role": "CASHIER", 
  "commissionRate": 10,
  "parentCashierId": "uuid-del-cajero-padre"
}
```

**Crear JUGADOR:**
```json
{
  "username": "player1",
  "password": "1234",
  "playerStatus": "ACTIVE",
  "email": "player@test.com",
  "initialBalance": 1000
}
```

#### **Response:**
```typescript
interface UserResponse {
  id: string;
  username: string;
  userType: 'SUPER_ADMIN' | 'BRAND_ADMIN' | 'CASHIER' | 'PLAYER'; // Calculado automáticamente
  status: string;
  brandId?: string;
  brandName?: string;
  createdByUserId?: string;
  createdByUsername?: string;
  createdAt: string;
  lastLoginAt?: string;
  
  // Campos de backoffice (solo si aplica)
  role?: 'SUPER_ADMIN' | 'BRAND_ADMIN' | 'CASHIER';
  commissionRate?: number;
  parentCashierId?: string;
  parentCashierUsername?: string;
  subordinatesCount?: number;
  
  // Campos de jugador (solo si aplica)
  email?: string;
  externalId?: string;
  balance?: number;
}
```

---

## ?? **2. LISTAR USUARIOS**

### `GET /api/v1/admin/users`

Lista usuarios según los permisos del usuario actual:

#### **Query Parameters:**
```typescript
interface QueryParams {
  page?: number;              // Default: 1
  pageSize?: number;          // Default: 20, Max: 100
  globalScope?: boolean;      // Solo SUPER_ADMIN puede usar true
  username?: string;          // Filtro por username (contiene)
  userType?: 'SUPER_ADMIN' | 'BRAND_ADMIN' | 'CASHIER' | 'PLAYER';
  status?: string;            // Filtro por status
  createdByUserId?: string;   // Filtro por creador
  parentCashierId?: string;   // Filtro por cashier padre
}
```

#### **Ejemplos:**

**SUPER_ADMIN - Ver todos los usuarios:**
```
GET /api/v1/admin/users?globalScope=true
```

**BRAND_ADMIN - Ver usuarios de su brand:**
```
GET /api/v1/admin/users
```

**CASHIER - Ver solo su jerarquía:**
```
GET /api/v1/admin/users
```

**Filtrar solo jugadores:**
```
GET /api/v1/admin/users?userType=PLAYER
```

**Buscar por username:**
```
GET /api/v1/admin/users?username=player
```

#### **Response:**
```typescript
interface UsersListResponse {
  users: UserResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

---

## ?? **3. OBTENER USUARIO**

### `GET /api/v1/admin/users/{userId}`

Obtiene los detalles de un usuario específico.

#### **Response:** 
Mismo formato que `UserResponse` del endpoint de creación.

---

## ?? **4. ACTUALIZAR USUARIO**

### `PATCH /api/v1/admin/users/{userId}`

Actualiza un usuario existente.

#### **Request Body:**
```typescript
interface UpdateUserRequest {
  username?: string;          // Nuevo username
  password?: string;          // Nueva contraseña
  status?: string;            // Nuevo status
  
  // Para usuarios de backoffice
  role?: 'SUPER_ADMIN' | 'BRAND_ADMIN' | 'CASHIER';
  commissionRate?: number;    // Nueva comisión
  
  // Para jugadores
  email?: string;             // Nuevo email
  playerStatus?: 'ACTIVE' | 'INACTIVE' | 'SUSPENDED';
}
```

#### **Ejemplo:**
```json
{
  "username": "nuevo-username",
  "commissionRate": 20
}
```

---

## ??? **5. ELIMINAR USUARIO**

### `DELETE /api/v1/admin/users/{userId}`

Elimina un usuario. No se puede eliminar si tiene subordinados.

#### **Response:**
```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

---

## ?? **6. AJUSTAR WALLET DE JUGADOR**

### `POST /api/v1/admin/users/{playerId}/wallet/adjust`

Ajusta el balance de un jugador. Solo funciona con usuarios tipo PLAYER.

#### **Request Body:**
```typescript
interface AdjustWalletRequest {
  amount: number;             // Positivo = crédito, Negativo = débito
  reason: string;             // Descripción del ajuste
}
```

#### **Ejemplo:**
```json
{
  "amount": 500,
  "reason": "Bonus por referido"
}
```

#### **Response:**
```typescript
interface WalletAdjustmentResponse {
  success: boolean;
  newBalance: number;
  ledgerEntryId: number;
  message: string;
}
```

---

## ?? **JERARQUÍA DE PERMISOS**

### **SUPER_ADMIN:**
- ? Ve **TODOS** los usuarios con `?globalScope=true`
- ? Ve usuarios de un brand específico sin globalScope
- ? Puede crear: SUPER_ADMIN, BRAND_ADMIN, CASHIER, PLAYER
- ? Puede modificar y eliminar cualquier usuario

### **BRAND_ADMIN:**
- ? Ve **TODOS** los usuarios de su brand (backoffice + jugadores)
- ? Puede crear: BRAND_ADMIN, CASHIER, PLAYER (en su brand)
- ? Puede modificar usuarios de su brand
- ? No puede crear SUPER_ADMIN

### **CASHIER:**
- ? Ve **SOLO** usuarios creados por él (jerarquía recursiva)
- ? Puede crear: CASHIER (subordinados), PLAYER
- ? Los cajeros que crea se auto-asignan como subordinados
- ? Los jugadores que crea se auto-asignan a él

---

## ?? **EJEMPLOS DE JERARQUÍA**

### **Cajero viendo su jerarquía:**
```
Cajero Principal (ve a todos estos):
??? Él mismo
??? Jugador 1 (creado por él)
??? Jugador 2 (creado por él)  
??? Cajero Subordinado (creado por él)
?   ??? Jugador 3 (creado por subordinado)
?   ??? Jugador 4 (creado por subordinado)
??? Otro Cajero Subordinado
    ??? Jugador 5 (creado por este subordinado)
```

---

## ?? **CÓDIGOS DE ERROR COMUNES**

### **400 Bad Request:**
- Username ya existe
- Datos de validación incorrectos
- Balance insuficiente para ajuste

### **403 Forbidden:**
- Sin permisos para crear el tipo de usuario
- Intentando ver usuarios fuera de su jerarquía
- Solo SUPER_ADMIN puede usar globalScope

### **404 Not Found:**
- Usuario no encontrado
- No tiene permisos para ver ese usuario

### **409 Conflict:**
- Username ya existe
- Usuario tiene subordinados (no se puede eliminar)

---

## ?? **CAMBIOS PRINCIPALES VS VERSIÓN ANTERIOR**

### ? **Simplificaciones:**
1. **Un solo endpoint** `/users` para todo
2. **Sin UserType en request** - se calcula automáticamente
3. **role en lugar de backofficeRole**
4. **createdByUserId en lugar de createdByCashierId**
5. **Contraseñas simples** en modo dev (mínimo 4 caracteres)

### ? **Lógica Automática:**
- **UserType** se determina por presencia de `role`
- **Auto-asignación** de subordinados para CASHIER
- **Jerarquía recursiva** para ver usuarios creados

### ? **Compatibilidad:**
- Todos los campos anteriores siguen disponibles
- Respuestas incluyen tanto `role` como `userType`
- Migraciones automáticas de datos existentes

---

## ?? **Testing Quick Start**

```bash
# 1. Crear SUPERADMIN
curl -X POST /api/v1/admin/users \
  -H "Content-Type: application/json" \
  -d '{"username":"super1","password":"1234","role":"SUPER_ADMIN"}'

# 2. Crear CAJERO  
curl -X POST /api/v1/admin/users \
  -H "Content-Type: application/json" \
  -d '{"username":"cajero1","password":"1234","role":"CASHIER","commissionRate":15}'

# 3. Crear JUGADOR
curl -X POST /api/v1/admin/users \
  -H "Content-Type: application/json" \
  -d '{"username":"player1","password":"1234","playerStatus":"ACTIVE","initialBalance":1000}'

# 4. Listar usuarios 
curl -X GET /api/v1/admin/users

# 5. Ajustar wallet
curl -X POST /api/v1/admin/users/{playerId}/wallet/adjust \
  -H "Content-Type: application/json" \
  -d '{"amount":500,"reason":"Bonus"}'
```