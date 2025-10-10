# API de Usuarios y Transacciones - Casino Platform

## Autenticaci�n
- Todas las rutas requieren JWT v�lido de backoffice (SUPER_ADMIN, BRAND_ADMIN, CASHIER).
- Enviar el token en el header: `Authorization: Bearer {token}` o en la cookie `bk.token`.

## Endpoints de Usuarios

### 1. Listar usuarios
**GET** `/api/v1/admin/users`

**Query params:**
- `userType`: "BACKOFFICE", "PLAYER" o vac�o (todos)
- `role`: "SUPER_ADMIN", "BRAND_ADMIN", "CASHIER", "PLAYER"
- `status`: "ACTIVE", "INACTIVE"
- `username`: filtra por username
- `globalScope=true`: solo para SUPER_ADMIN, lista todos los brands
- `page`, `pageSize`: paginaci�n

**Respuesta ejemplo:**
```json
{
  "data": [
    {
      "id": "uuid",
      "userType": "BACKOFFICE" | "PLAYER",
      "username": "nombreusuario",
      "email": "email@ejemplo.com",
      "role": "CASHIER",
      "status": "ACTIVE",
      "brandId": "uuid",
      "brandName": "Brand Name",
      "walletBalance": 1000.00,
      "createdAt": "2024-02-07T...",
      "createdByUserId": "uuid",
      "createdByUsername": "admin",
      "createdByRole": "BRAND_ADMIN"
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "appliedScope": "brand:..."
}
```

---

### 2. Ver detalles de usuario
**GET** `/api/v1/admin/users/{userId}`

**Respuesta:** Igual a un usuario del listado, pero con todos los campos.

---

### 3. Crear usuario
**POST** `/api/v1/admin/users`

**Body ejemplo (crear cajero):**
```json
{
  "username": "cajero2",
  "password": "password123",
  "role": 2,                // 0: SUPER_ADMIN, 1: BRAND_ADMIN, 2: CASHIER, 3: PLAYER
  "commissionPercent": 0    // Solo para CASHIER, opcional para otros roles
}
```

**Notas:**
- Si el usuario autenticado es CASHIER y crea otro cajero, el backend asigna autom�ticamente el parentCashierId.
- Para crear PLAYER, solo env�a username, password, email (opcional), role: 3 o sin role.

---

### 4. Modificar usuario
**PATCH** `/api/v1/admin/users/{userId}`

**Body ejemplo:**
```json
{
  "username": "nuevoNombre",
  "password": "nuevoPassword",
  "role": "CASHIER",
  "status": "ACTIVE"
}
```

---

### 5. Eliminar usuario
**DELETE** `/api/v1/admin/users/{userId}`

---

### 6. Buscar usuario por username
**GET** `/api/v1/admin/users/search?username=nombre`

---

### 7. Consultar balance de usuario
**GET** `/api/v1/admin/users/{userId}/balance?userType=BACKOFFICE|PLAYER`

---

## Endpoints de Transacciones (Wallet)

### 1. Listar transacciones
**GET** `/api/v1/admin/transactions`

**Query params:**
- `userId`: filtra por usuario origen o destino
- `userType`: "BACKOFFICE" o "PLAYER"
- `fromDate`, `toDate`: filtra por fechas
- `description`: filtra por descripci�n
- `globalScope=true`: solo SUPER_ADMIN
- `page`, `pageSize`: paginaci�n

**Respuesta ejemplo:**
```json
{
  "transactions": [
    {
      "id": "uuid",
      "type": "TRANSFER" | "MINT",
      "fromUserId": "uuid",
      "fromUserType": "BACKOFFICE" | "PLAYER",
      "fromUsername": "admin",
      "toUserId": "uuid",
      "toUserType": "PLAYER",
      "toUsername": "jugador1",
      "amount": 500.00,
      "previousBalanceFrom": 1000.00,
      "newBalanceFrom": 500.00,
      "previousBalanceTo": 0.00,
      "newBalanceTo": 500.00,
      "description": "Recarga",
      "createdByUserId": "uuid",
      "createdByUsername": "admin",
      "createdByRole": "SUPER_ADMIN",
      "createdAt": "2024-02-07T..."
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### 2. Enviar o quitar balance (transferencia)
**POST** `/api/v1/admin/transactions`

**Body ejemplo para enviar:**
```json
{
  "fromUserId": "uuid_origen",
  "fromUserType": "BACKOFFICE",
  "toUserId": "uuid_destino",
  "toUserType": "PLAYER",
  "amount": 100.00,
  "idempotencyKey": "unico-por-operacion-123",
  "description": "Recarga"
}
```

**Para quitar balance:**
Invierte `fromUserId` y `toUserId` y pon el monto positivo.

---

### 3. Consultar balance de usuario
**GET** `/api/v1/admin/users/{userId}/balance?userType=BACKOFFICE|PLAYER`

---

## Notas para el Frontend
- El backend resuelve el brand autom�ticamente.
- El JWT debe enviarse en cada request.
- El campo `idempotencyKey` en transferencias debe ser �nico por operaci�n.
- Para obtener el n�mero de dep�sitos, cuenta las transacciones donde el usuario es `toUserId` y el tipo es `TRANSFER` o `MINT`.
- Cada usuario del listado debe ser clickeable y navegar a `/users/{id}` para ver detalles y transacciones.
- El sidebar debe tener una secci�n para transacciones (`/transactions`).

---

## Prompt sugerido para Sonnet 4.5

> Implementa la UI de usuarios y transacciones usando la documentaci�n anterior. En el listado de usuarios muestra: nombre de usuario, fichas/balance, botones + y - para enviar/quitar balance, n�mero de dep�sitos, opciones de modificar/eliminar. Cada usuario debe ser clickeable y navegar a `/users/{id}` mostrando detalles y transacciones. Agrega una secci�n de transacciones en el sidebar.
