# Unified Users API Endpoints

## Base Path

```
/api/v1/admin/users
```

---

## 1. Crear usuario (Backoffice o Player)

**POST** `/api/v1/admin/users`

### Body (JSON)
```json
{
  "username": "string",
  "password": "string (opcional para player, requerido para backoffice)",
  "email": "string (solo player)",
  "externalId": "string (solo player)",
  "role": "SUPER_ADMIN | BRAND_ADMIN | CASHIER | PLAYER (null = PLAYER)",
  "parentCashierId": "guid (solo CASHIER)",
  "commissionPercent": "decimal (solo CASHIER, 0-100)"
}
```

---

## 2. Listar usuarios (Backoffice + Players)

**GET** `/api/v1/admin/users`

### Query Params

- `username` (string, opcional): Filtrar por username
- `userType` (string, opcional): "BACKOFFICE", "PLAYER" o vacío para ambos
- `role` (string, opcional): Filtrar por rol ("SUPER_ADMIN", "BRAND_ADMIN", "CASHIER", "PLAYER")
- `status` (string, opcional): Filtrar por estado
- `createdFrom` (ISO date, opcional): Fecha de creación desde
- `createdTo` (ISO date, opcional): Fecha de creación hasta
- `globalScope` (bool, opcional, default: false): Solo para SUPER_ADMIN, ver todos los usuarios
- `page` (int, opcional, default: 1): Página de resultados
- `pageSize` (int, opcional, default: 20): Tamaño de página

---

## 3. Ver detalles de usuario

**GET** `/api/v1/admin/users/{userId}`

- `userId` (guid): ID del usuario (puede ser backoffice o player)

---

## 4. Editar usuario

**PATCH** `/api/v1/admin/users/{userId}`

- `userId` (guid): ID del usuario

### Body (JSON)
```json
{
  "username": "string (opcional)",
  "password": "string (opcional, solo backoffice)",
  "email": "string (opcional, solo player)",
  "role": "SUPER_ADMIN | BRAND_ADMIN | CASHIER | PLAYER (opcional, solo SUPER_ADMIN)",
  "status": "string (opcional)",
  "commissionPercent": "decimal (opcional, solo CASHIER)"
}
```

---

## 5. Eliminar usuario

**DELETE** `/api/v1/admin/users/{userId}`

- `userId` (guid): ID del usuario

---

## 6. Buscar usuario por username

**GET** `/api/v1/admin/users/search`

### Query Params

- `username` (string, requerido): Username exacto a buscar

---

## Resumen de Query Params para GET /users

| Param           | Tipo     | Descripción                                 |
|-----------------|----------|---------------------------------------------|
| username        | string   | Filtrar por username                        |
| userType        | string   | "BACKOFFICE", "PLAYER" o ambos              |
| role            | string   | Rol de usuario (SUPER_ADMIN | BRAND_ADMIN | CASHIER | PLAYER )            
| status          | string   | Estado del usuario                          |
| createdFrom     | date     | Fecha de creación desde (ISO)               |
| createdTo       | date     | Fecha de creación hasta (ISO)               |
| globalScope     | bool     | Solo SUPER_ADMIN, ver todos los usuarios    |
| page            | int      | Página de resultados                        |
| pageSize        | int      | Tamaño de página                            |

---

**Notas:**
- Todos los endpoints requieren autorización de backoffice.
- El endpoint `/users/search` busca por username en ambas tablas (backoffice y players).
- El endpoint `/users` soporta paginación y filtros avanzados.
- El campo `role` en creación/edición determina el tipo de usuario (backoffice o player).
