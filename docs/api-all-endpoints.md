# API - Endpoints Expuestos (v1)

## Autenticación
- Todas las rutas requieren JWT válido de backoffice (SUPER_ADMIN, BRAND_ADMIN, CASHIER).
- Token en header: `Authorization: Bearer {token}` o cookie `bk.token`.

---

## Usuarios (Unificado)

- **POST** `/api/v1/admin/users` — Crear usuario (backoffice o player)
- **GET** `/api/v1/admin/users` — Listar usuarios (query: userType, role, status, username, globalScope, page, pageSize)
- **GET** `/api/v1/admin/users/{userId}` — Ver detalles de usuario
- **PATCH** `/api/v1/admin/users/{userId}` — Modificar usuario
- **DELETE** `/api/v1/admin/users/{userId}` — Eliminar usuario
- **GET** `/api/v1/admin/users/search?username=nombre` — Buscar usuario por username
- **GET** `/api/v1/admin/users/{userId}/balance?userType=BACKOFFICE|PLAYER` — Consultar balance de usuario

---

## Transacciones (Wallet de usuarios backoffice)

- **POST** `/api/v1/admin/transactions` — Crear transacción (MINT o TRANSFER)
- **GET** `/api/v1/admin/transactions` — Listar transacciones (query: userId, userType, fromDate, toDate, description, globalScope, page, pageSize)

---

## Brands

- **POST** `/api/v1/admin/brands` — Crear brand
- **GET** `/api/v1/admin/brands` — Listar brands
- **GET** `/api/v1/admin/brands/{brandId}` — Ver detalles de brand
- **PATCH** `/api/v1/admin/brands/{brandId}` — Modificar brand
- **DELETE** `/api/v1/admin/brands/{brandId}` — Eliminar brand
- **POST** `/api/v1/admin/brands/{brandId}/status` — Cambiar estado de brand

### Brand Settings
- **GET** `/api/v1/admin/brands/{brandId}/settings` — Obtener settings
- **PUT** `/api/v1/admin/brands/{brandId}/settings` — Reemplazar settings
- **PATCH** `/api/v1/admin/brands/{brandId}/settings` — Modificar settings

### Brand Provider Config
- **GET** `/api/v1/admin/brands/{brandId}/providers` — Listar providers
- **PUT** `/api/v1/admin/brands/{brandId}/providers/{providerCode}` — Modificar config de provider
- **POST** `/api/v1/admin/brands/{brandId}/providers/{providerCode}/rotate-secret` — Rotar secreto

### Utilidades de Brand
- **GET** `/api/v1/admin/brands/by-host/{host}` — Buscar brand por host
- **GET** `/api/v1/admin/brands/{brandId}/catalog` — Catálogo de juegos del brand

---

## Otros (Legacy/Internos wallet conectado con juegos)

- **POST** `/api/v1/internal/wallet/balance` — Consultar balance (legacy, para gateway)
- **POST** `/api/v1/internal/wallet/debit` — Debitar (legacy)
- **POST** `/api/v1/internal/wallet/credit` — Acreditar (legacy)
- **POST** `/api/v1/internal/wallet/rollback` — Rollback de transacción (legacy)

---

## Notas
- Todos los endpoints devuelven errores claros en formato JSON.
- El backend resuelve el brand automáticamente por dominio/host.
- El scope y permisos se validan según el rol y el brand asignado.
- Para detalles de request/response, ver la documentación específica de cada módulo.
