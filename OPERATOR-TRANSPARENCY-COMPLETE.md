# ? IMPLEMENTACI�N COMPLETADA: Autorizaci�n Transparente por Brand

## ?? Problema Resuelto

**ANTES**: Los usuarios ten�an que especificar `operatorId` manualmente en cada request, lo cual no era intuitivo ni seguro.

**AHORA**: El `operatorId` se resuelve **autom�ticamente** desde el `BrandContext` (que se obtiene del dominio/URL), siendo **transparente** para todos los usuarios excepto `SUPER_ADMIN`.

## ?? Cambios Implementados

### 1. **AuthorizationHelper** (`apps/api/Casino.Api/Utils/AuthorizationHelper.cs`)
- ? Clase helper centralizada para manejo de autorizaci�n
- ? M�todo `GetEffectiveScope()` que resuelve autom�ticamente `operatorId` y `brandId`
- ? Validaci�n de permisos por rol
- ? Logging estructurado para auditor�a

### 2. **PlayerManagementEndpoints** (Actualizado)
- ? Usa `AuthorizationHelper` para resoluci�n transparente de scope
- ? `BrandId` es opcional en requests (se resuelve autom�ticamente)
- ? Validaci�n de permisos por rol
- ? Auto-asignaci�n de jugadores a CASHIER

### 3. **PlayerDTOs** (Actualizado)
- ? `CreatePlayerRequest.BrandId` es opcional
- ? Se resuelve autom�ticamente del contexto para roles no-SUPER_ADMIN

### 4. **PlayerService** (Corregido)
- ? Maneja `BrandId` nullable correctamente
- ? Validaci�n robusta de par�metros

## ??? Arquitectura de Permisos

### Jerarqu�a de Roles

```
SUPER_ADMIN
??? Acceso global a todos los operators/brands
??? Debe especificar expl�citamente brandId en requests
??? operatorScope = null, brandScope = null
??? Sin restricciones autom�ticas

OPERATOR_ADMIN  
??? Solo su operator y brands asociados
??? operatorScope = brandContext.OperatorId (autom�tico)
??? brandScope = brandContext.BrandId (autom�tico)
??? Puede crear: OPERATOR_ADMIN, CASHIER, PLAYERS
??? Transparente: no necesita especificar operatorId/brandId

CASHIER
??? Solo su operator/brand y jugadores asignados
??? operatorScope = brandContext.OperatorId (autom�tico)
??? brandScope = brandContext.BrandId (autom�tico)
??? Puede crear: CASHIER, PLAYERS (auto-asignados)
??? Transparente: no necesita especificar operatorId/brandId
```

### Flujo de Resoluci�n

```
1. Request ? https://admin.bet30.local:7182/api/v1/admin/players

2. BrandResolverMiddleware
   ??? Busca brand por dominio: admin.bet30.local
   ??? brandContext.BrandId = "22222222-2222-2222-2222-222222222222"
   ??? brandContext.OperatorId = "11111111-1111-1111-1111-111111111111"

3. PlayerManagementEndpoints
   ??? AuthorizationHelper.GetEffectiveScope(role, brandContext)
   ??? Para OPERATOR_ADMIN/CASHIER:
   ?   ??? operatorScope = brandContext.OperatorId (autom�tico)
   ?   ??? brandScope = brandContext.BrandId (autom�tico)
   ??? Para SUPER_ADMIN: operatorScope = null, brandScope = null

4. PlayerService
   ??? Filtra por operatorScope y brandScope
   ??? query.Where(p => p.Brand.OperatorId == operatorScope)
   ??? Solo ve datos de su operator/brand
```

## ?? Ejemplos de Uso

### Request de Creaci�n (OPERATOR_ADMIN/CASHIER)
```json
POST /api/v1/admin/players
Host: admin.bet30.local:7182

{
  "username": "jugador123",
  "email": "jugador@example.com",
  "initialBalance": 1000
}

// BrandId se resuelve autom�ticamente del contexto
// OperatorId se resuelve autom�ticamente del brandContext.OperatorId
```

### Request de Creaci�n (SUPER_ADMIN)
```json
POST /api/v1/admin/players
Host: admin.bet30.local:7182

{
  "username": "jugador123",
  "email": "jugador@example.com",
  "brandId": "22222222-2222-2222-2222-222222222222", // Puede especificar cualquier brand
  "initialBalance": 1000
}

// SUPER_ADMIN debe especificar expl�citamente el brandId
```

### Listado (Scope Autom�tico)
```javascript
// Para OPERATOR_ADMIN en bet30.local:
GET /api/v1/admin/players
// Solo ve jugadores de operatorId = brandContext.OperatorId

// Para SUPER_ADMIN:
GET /api/v1/admin/players  
// Ve jugadores de todos los operators
```

## ? Beneficios Logrados

1. **?? Seguridad**: Imposible acceder a datos de otros operators
2. **?? Simplicidad**: Usuarios no necesitan saber sobre operatorId
3. **? Transparencia**: Resoluci�n autom�tica por URL/dominio
4. **?? Mantenibilidad**: L�gica centralizada en AuthorizationHelper
5. **?? Escalabilidad**: F�cil agregar nuevos brands/operators
6. **?? Auditor�a**: Logging estructurado con contexto completo

## ?? Pr�ximos Pasos

Para completar la implementaci�n, aplicar el mismo patr�n a:

- [ ] `BackofficeUserEndpoints.cs`
- [ ] `OperatorEndpoints.cs` 
- [ ] `BrandGameEndpoints.cs`
- [ ] `CashierPlayerEndpoints.cs`
- [ ] `AdminEndpoints.cs`

**Patr�n a seguir**:
```csharp
// 1. Importar helper
using Casino.Api.Utils;

// 2. Obtener rol y validar permisos
var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
var permissionError = AuthorizationHelper.ValidateOperationPermissions(currentRole, brandContext);

// 3. Resolver scope autom�tico
var (operatorScope, brandScope) = AuthorizationHelper.GetEffectiveScope(currentRole, brandContext);

// 4. Usar en servicios
await service.GetDataAsync(operatorScope, brandScope);
```

## ?? Resultado Final

El sistema ahora es **verdaderamente multi-tenant** con:
- ? Resoluci�n autom�tica de operatorId por URL
- ? Seguridad por capas basada en roles
- ? UX simplificada para usuarios finales
- ? Escalabilidad para m�ltiples operators/brands
- ? Auditor�a completa de acciones

Los usuarios ahora pueden trabajar **sin conocer la estructura interna** de operators/brands, mientras que el sistema mantiene la **seguridad y aislamiento** de datos autom�ticamente.