# Multi-Site Implementation Guide

## ? Implementaci�n Completada

Se ha implementado completamente el soporte multi-site en la plataforma Casino siguiendo las especificaciones de TENANCY.md.

### ??? **Componentes Implementados**

#### 1. **BrandContext Service** 
- ? Servicio scoped que mantiene el contexto del brand actual
- ? Propiedades: `BrandId`, `BrandCode`, `Domain`, `CorsOrigins`, `OperatorId`
- ? M�todo `IsResolved` para verificar si el brand fue resuelto

#### 2. **BrandResolverMiddleware**
- ? Intercepta requests y resuelve brand por `Request.Host.Host`
- ? Busca en BD por `brand.domain` o `brand.admin_domain`
- ? Setea valores en `BrandContext` si encuentra el brand
- ? Responde 400 `{ error: "brand_not_resolved" }` si no encuentra
- ? Verifica que el brand est� activo (status = ACTIVE)
- ? Skip autom�tico para rutas que no necesitan resoluci�n (/health, /swagger, /gateway/*)

#### 3. **DynamicCorsMiddleware**  
- ? CORS din�mico basado en `brand.cors_origins`
- ? Verifica origin contra lista permitida del brand
- ? Responde 403 si origin no est� permitido
- ? Permite CORS permisivo para rutas de sistema (/health, /swagger)
- ? Soporte para preflight requests (OPTIONS)

#### 4. **Database Schema**
- ? Nuevas columnas en tabla `brand`:
  - `domain` (UNIQUE) - dominio principal
  - `admin_domain` (UNIQUE) - dominio admin opcional  
  - `cors_origins` - array de or�genes permitidos
- ? �ndices �nicos para dominios
- ? Migraci�n aplicada: `AddBrandDomainSupport`

#### 5. **Catalog Endpoints** (`/api/v1/catalog/*`)
- ? `GET /games` - devuelve solo juegos del brand actual
- ? `POST /games/{gameCode}/launch` - crea session asociada al brand
- ? Filtrado autom�tico por `BrandContext.BrandId`
- ? Validaci�n de jugador pertenece al brand
- ? Validaci�n de juego disponible para el brand

#### 6. **Admin Endpoints Actualizados**
- ? Todos los endpoints admin ahora usan `BrandContext`
- ? Filtrado autom�tico por brand actual (no m�s `?brandId=`)
- ? `GET /admin/players` - solo jugadores del brand actual
- ? `POST /admin/players/{id}/wallet/adjust` - solo jugadores del brand
- ? Auditor�a incluye informaci�n del brand

#### 7. **Gateway Endpoints** (sin cambios)
- ? Mantienen resoluci�n por `sessionId`/`playerId` como especificado
- ? No usan resoluci�n por host
- ? Siguen funcionando con HMAC como antes

### ?? **C�mo Usar**

#### 1. **Configurar Brands en BD**
```sql
-- Usar el script incluido
\i setup-multisite-data.sql
```

#### 2. **Endpoints por Dominio**

**Para localhost:3000 (brand LOCAL):**
```bash
# Ver cat�logo del brand LOCAL
GET http://localhost:5000/api/v1/catalog/games
Host: localhost:3000

# Lanzar juego para brand LOCAL  
POST http://localhost:5000/api/v1/catalog/games/SLOTS_001/launch
Host: localhost:3000
{
  "playerId": "d1111111-1111-1111-1111-111111111111",
  "expirationMinutes": 60
}

# Ver jugadores del brand LOCAL
GET http://localhost:5000/api/v1/admin/players  
Host: localhost:3000
```

**Para test.casino.com (brand TEST):**
```bash
# Ver cat�logo del brand TEST (diferentes juegos)
GET http://localhost:5000/api/v1/catalog/games
Host: test.casino.com

# Solo ver� jugadores del brand TEST
GET http://localhost:5000/api/v1/admin/players
Host: test.casino.com
```

#### 3. **CORS Din�mico**
```javascript
// Frontend en localhost:3000 para brand LOCAL
fetch('http://localhost:5000/api/v1/catalog/games', {
  headers: {
    'Origin': 'http://localhost:3000' // Debe estar en brand.cors_origins
  }
})
```

#### 4. **Gateway (sin cambios)**
```bash
# Los endpoints gateway siguen igual
POST http://localhost:5000/api/v1/gateway/bet
Headers: X-Provider: demo, X-Signature: xxx
{
  "sessionId": "xxx",
  "playerId": "xxx", 
  "amount": 1000
}
```

### ?? **Validaciones Implementadas**

1. **Brand Resolution**: 400 si host no se encuentra en BD
2. **Brand Status**: 403 si brand no est� ACTIVE  
3. **CORS Validation**: 403 si origin no est� permitido
4. **Player Ownership**: Endpoints admin verifican que jugador pertenezca al brand
5. **Game Availability**: Catalog verifica que juego est� asignado al brand
6. **Operator Scoping**: Admin endpoints filtran por operador del brand

### ?? **Ejemplos de Datos**

El script `setup-multisite-data.sql` crea:

- **3 Brands**: LOCAL, TEST, BET30 con diferentes dominios
- **3 Games**: Slots, Poker, Roulette 
- **Game Assignments**: Cada brand tiene diferentes juegos disponibles
- **4 Players**: Distribuidos entre brands
- **Initial Balances**: Saldos de prueba

### ??? **Arquitectura**

```
Request con Host: localhost:3000
    ?
BrandResolverMiddleware ? BrandContext.BrandId = LOCAL_BRAND_ID  
    ?
DynamicCorsMiddleware ? Verifica CORS para localhost:3000
    ?  
CatalogEndpoints ? Devuelve solo juegos del LOCAL_BRAND_ID
```

### ? **Testing**

1. **Diferentes Hosts**: Cambiar `Host` header para probar brands diferentes
2. **CORS**: Probar con diferentes `Origin` headers  
3. **Data Isolation**: Verificar que cada brand solo ve sus datos
4. **Gateway**: Verificar que gateway sigue funcionando sin host resolution

### ?? **Configuraci�n en Production**

1. Configurar dominios reales en tabla `brands`
2. Configurar `cors_origins` apropiados para cada brand
3. DNS pointing hacia la misma API pero con diferentes hosts
4. Load balancer/reverse proxy que preserve `Host` header

### ?? **Standards Mantenidos**

- ? Minimal APIs
- ? DTOs como `record`  
- ? `TypedResults` 
- ? FluentValidation
- ? Structured logging con brand info
- ? ProblemDetails para errores
- ? C�digo limpio y documentado

**La implementaci�n multi-site est� completa y lista para uso! ??**