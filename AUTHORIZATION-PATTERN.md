# ?? Patr�n de Autorizaci�n Transparente por Brand

## ?? Problema Identificado

El sistema requer�a que los usuarios especificaran `operatorId` expl�citamente en sus requests, pero esto debe ser **transparente** para todos los roles excepto `SUPER_ADMIN`. El `operatorId` debe resolverse autom�ticamente desde el **brand context** (que se resuelve por la URL/dominio).

## ? Soluci�n Implementada

### Jerarqu�a de Roles y Permisos

```
SUPER_ADMIN
??? Ve y gestiona TODOS los operators y brands
??? Debe especificar expl�citamente brandId/operatorId en requests
??? No tiene restricciones autom�ticas
??? Acceso global

OPERATOR_ADMIN
??? Ve y gestiona solo SU operator y brands asociados
??? operatorId/brandId se resuelve autom�ticamente del contexto
??? Puede crear: m�s OPERATOR_ADMIN, CASHIER, PLAYERS
??? Scope autom�tico: su operator/brand

CASHIER
??? Ve y gestiona solo SU operator/brand y jugadores asignados
??? operatorId/brandId se resuelve autom�ticamente del contexto
??? Puede crear: m�s CASHIER, PLAYERS (se auto-asignan)
??? Puede ajustar wallets de jugadores asignados
??? Scope autom�tico: su operator/brand + solo sus jugadores
```

## ?? Implementaci�n

### 1. AuthorizationHelper (`apps/api/Casino.Api/Utils/AuthorizationHelper.cs`)

Clase helper que encapsula toda la l�gica de autorizaci�n:

```csharp
// Resolver scope autom�ticamente
var (operatorScope, brandScope) = AuthorizationHelper.GetEffectiveScope(currentRole, brandContext);

// Para SUPER_ADMIN: (null, null) = sin restricciones
// Para otros roles: (brandContext.OperatorId, brandContext.BrandId) = scope autom�tico
```

### 2. Validaci�n de Permisos

```csharp
// Validar que el usuario tenga permisos para la operaci�n
var permissionError = AuthorizationHelper.ValidateOperationPermissions(
    currentRole, 
    brandContext,
    requiredRoles: new[] { BackofficeUserRole.SUPER_ADMIN, BackofficeUserRole.OPERATOR_ADMIN },
    requireBrandContext: true);

if (permissionError != null)
    return permissionError; // Retorna 403 o 400
```

### 3. BrandId Efectivo para Creaci�n

```csharp
// Para operaciones que crean entidades con brandId
var effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
    currentRole, 
    brandContext, 
    request.BrandId); // Solo SUPER_ADMIN puede especificar diferente

// Para otros roles: siempre usa brandContext.BrandId (transparente)
```

## ?? C�mo Aplicar a Otros Endpoints

### Ejemplo: BackofficeUserEndpoints

```csharp
private static async Task<IResult> CreateUser(
    [FromBody] CreateBackofficeUserRequest request,
    IBackofficeUserService userService,
    BrandContext brandContext,
    HttpContext httpContext,
    ILogger<Program> logger)
{
    var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
    
    // Validar permisos
    var permissionError = AuthorizationHelper.ValidateOperationPermissions(
        currentRole, 
        brandContext,
        requiredRoles: new[] { BackofficeUserRole.SUPER_ADMIN, BackofficeUserRole.OPERATOR_ADMIN });
    
    if (permissionError != null)
        return permissionError;
    
    // Resolver operatorId autom�ticamente
    var effectiveOperatorId = currentRole == BackofficeUserRole.SUPER_ADMIN 
        ? request.OperatorId // SUPER_ADMIN puede especificar cualquiera
        : brandContext.OperatorId; // Otros usan el del contexto
    
    var effectiveRequest = request with { OperatorId = effectiveOperatorId };
    
    var response = await userService.CreateUserAsync(effectiveRequest, currentUserId);
    
    // ... resto de la l�gica
}
```

### Ejemplo: Listado con Scope

```csharp
private static async Task<IResult> GetUsers(
    IBackofficeUserService userService,
    BrandContext brandContext,
    HttpContext httpContext)
{
    var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
    var (operatorScope, brandScope) = AuthorizationHelper.GetEffectiveScope(currentRole, brandContext);
    
    // El servicio usa operatorScope para filtrar autom�ticamente
    var response = await userService.GetUsersAsync(operatorScope);
    
    return TypedResults.Ok(response);
}
```

## ?? Transparencia del OperatorId

### ? ANTES (Manual)
```json
// El usuario ten�a que especificar operatorId
POST /api/v1/admin/players
{
  "username": "player1",
  "operatorId": "11111111-1111-1111-1111-111111111111", // ? Manual
  "brandId": "22222222-2222-2222-2222-222222222222"
}
```

### ? AHORA (Transparente)
```json
// El operatorId se resuelve autom�ticamente del brand context
POST /api/v1/admin/players
{
  "username": "player1"
  // brandId se resuelve autom�ticamente del contexto (excepto SUPER_ADMIN)
  // operatorId se resuelve autom�ticamente del brandContext.OperatorId
}
```

## ?? Flujo de Resoluci�n

1. **Request llega** ? `https://admin.bet30.local:7182/api/v1/admin/players`

2. **BrandResolverMiddleware** ? Resuelve `bet30` brand por dominio
   ```csharp
   brandContext.BrandId = "22222222-2222-2222-2222-222222222222"
   brandContext.OperatorId = "11111111-1111-1111-1111-111111111111"
   ```

3. **Endpoint** ? Usa AuthorizationHelper
   ```csharp
   var (operatorScope, brandScope) = GetEffectiveScope(role, brandContext);
   // Para OPERATOR_ADMIN/CASHIER: 
   // operatorScope = "11111111-1111-1111-1111-111111111111" (autom�tico)
   // brandScope = "22222222-2222-2222-2222-222222222222" (autom�tico)
   ```

4. **Servicio** ? Filtra por scope autom�ticamente
   ```csharp
   query = query.Where(p => p.Brand.OperatorId == operatorScope);
   ```

## ? Beneficios

1. **Transparencia**: Usuarios no saben del operatorId, es autom�tico
2. **Seguridad**: Imposible acceder a datos de otros operators
3. **Simplicidad**: Menos par�metros en requests
4. **Escalabilidad**: F�cil agregar nuevos brands/operators
5. **Mantenibilidad**: L�gica centralizada en AuthorizationHelper

## ?? Endpoints que Necesitan Actualizaci�n

- [ ] `BackofficeUserEndpoints.cs`
- [ ] `OperatorEndpoints.cs`
- [ ] `BrandGameEndpoints.cs`
- [ ] `CashierPlayerEndpoints.cs`
- [x] `PlayerManagementEndpoints.cs` ?
- [ ] `AdminEndpoints.cs`

Usar el patr�n del `AuthorizationHelper` en todos estos endpoints para consistencia.