# Tenancy y Resolución de Brand por Host

## Objetivo
Cada site (casinoA.com, casinoB.com, bet30test.netlify.app, etc.) debe resolverse automáticamente según el **dominio (Host)** de la request.  
El front y el backoffice **no deben enviar brandId ni brandCode**. El backend debe resolver el `brand` en base al Host.

## Cambios en base de datos
- Agregar columnas a la tabla `brand`:
  - `domain text UNIQUE` → dominio principal del site (ej. `bet30test.netlify.app`)
  - `admin_domain text UNIQUE` → dominio opcional para backoffice (ej. `admin.bet30test.netlify.app`)
  - `cors_origins text[]` → lista de orígenes permitidos

## Reglas
- Cada request web (front o admin) se resuelve en el backend a un **BrandContext**.
- El BrandContext debe exponer:
  - `BrandId`
  - `BrandCode`
  - `Domain`
- Si el host no se encuentra en la tabla `brand`, devolver error 400 (`brand_not_resolved`).
- CORS debe ser dinámico: solo se permiten orígenes configurados en `brand.cors_origins`.
- Endpoints de catálogo, jugadores, ajustes, etc. deben filtrar datos por `BrandContext.BrandId`.
- Endpoints de gateway de proveedores (ej. `/api/v1/gateway/bet`) deben resolver el brand a partir de la `sessionId` o `playerId`, no del host.

## Middleware esperado
- `BrandResolverMiddleware`: intercepta cada request, busca el `brand` por host, y setea los valores en un servicio `BrandContext` (scoped).
- Debe inyectarse `BrandContext` en los endpoints para usar el brand resuelto.
