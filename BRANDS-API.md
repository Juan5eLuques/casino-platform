# Brands & Site Configuration API

## Objetivo
Exponer endpoints de administración para:
- Crear/editar **brands** (sites).
- Configurar **dominios** (front y backoffice) y **CORS** por brand.
- Activar/desactivar brand.
- Configurar **proveedores por brand** (secreto HMAC, flags como `allow_negative_on_rollback`).
- Ver/actualizar **settings** del brand (JSON de preferencias).
- Listar y consultar brands (scopado por rol/operador).

> El front/backoffice **no** envía brandId: el backend resuelve por `Host` (BrandResolver Middleware).

---

## Modelo de datos (resumen)
Tabla `brand` (PostgreSQL):
- `id uuid PK`
- `operator_id uuid FK`
- `code text UNIQUE`
- `name text`
- `domain text UNIQUE` (p.ej. `bet30.local` / `bet30test.netlify.app`)
- `admin_domain text UNIQUE` (opcional; p.ej. `admin.bet30.local`)
- `cors_origins text[]` (lista de origins permitidos)
- `status text` (`ACTIVE/INACTIVE`)
- `theme jsonb` (opcional)
- `settings jsonb` (config avanzada por site)
- timestamps…

Tabla `brand_provider_config` (nueva):
- `brand_id uuid FK`
- `provider_code text` (p.ej. `dummy`, `belatra`)
- `secret text` (HMAC)
- `allow_negative_on_rollback boolean default false`
- `meta jsonb` (cualquier otra config)
- PK `(brand_id, provider_code)`

Índices sugeridos:
- `UNIQUE(domain)`, `UNIQUE(admin_domain)`, `UNIQUE(code)`
- `brand_provider_config` PK compuesta

---

## Seguridad & Roles
- `SUPER_ADMIN`: puede CRUD de **todos** los brands.
- `OPERATOR_ADMIN`: CRUD **sólo** brands de su `operator_id`.
- `CASHIER`: sin acceso a brands.
- Todas las lecturas/escrituras deben validar el **alcance** por rol/operador.

---

## Endpoints

### 1) Brands (CRUD & estado)
**POST** `/api/v1/admin/brands`  
**GET** `/api/v1/admin/brands`  
**GET** `/api/v1/admin/brands/{brandId}`  
**PATCH** `/api/v1/admin/brands/{brandId}`  
**DELETE** `/api/v1/admin/brands/{brandId}`  
**POST** `/api/v1/admin/brands/{brandId}/status`

### 2) Brand Settings (JSON aislado)
**GET** `/api/v1/admin/brands/{brandId}/settings`  
**PUT** `/api/v1/admin/brands/{brandId}/settings`  
**PATCH** `/api/v1/admin/brands/{brandId}/settings`

### 3) Brand Provider Config
**GET** `/api/v1/admin/brands/{brandId}/providers`  
**PUT** `/api/v1/admin/brands/{brandId}/providers/{providerCode}`  
**POST** `/api/v1/admin/brands/{brandId}/providers/{providerCode}/rotate-secret`

### 4) Utilidades
**GET** `/api/v1/admin/brands/by-host/{host}`  
**GET** `/api/v1/admin/brands/{brandId}/catalog`

---

## Validaciones & Errores
- `409` si `code/domain/adminDomain` ya existen.
- `400` si `corsOrigins` inválido.
- `403` si operador no coincide.
- `404` si brand no existe o fuera de alcance.
- `422` validación DTO.

---

## Auditoría
- `BRAND_CREATE`, `BRAND_UPDATE`, `BRAND_STATUS_UPDATE`
- `BRAND_PROVIDER_CONFIG_UPSERT`, `BRAND_PROVIDER_SECRET_ROTATE`
- `BRAND_SETTINGS_PUT/PATCH`

---

## Concurrencia / Cache
- Cache de brands invalidado en cambios de `domain`, `admin_domain`, `cors_origins`, provider configs.
