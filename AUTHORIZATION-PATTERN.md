# ?? Patrón de Autorización Transparente por Brand

## ?? Problema Identificado

El sistema requería que los usuarios especificaran `operatorId` explícitamente en sus requests, pero esto debe ser **transparente** para todos los roles excepto `SUPER_ADMIN`. El `operatorId` debe resolverse automáticamente desde el **brand context** (que se resuelve por la URL/dominio).

## ? Solución Implementada

### Jerarquía de Roles y Permisos

```
SUPER_ADMIN
??? Ve y gestiona TODOS los operators y brands
??? Debe especificar explícitamente brandId/operatorId en requests
??? No tiene restricciones automáticas
??? Acceso global

OPERATOR_ADMIN
??? Ve y gestiona solo SU operator y brands asociados
??? operatorId/brandId se resuelve automáticamente del contexto
??? Puede crear: más OPERATOR_ADMIN, CASHIER, PLAYERS
??? Scope automático: su operator/brand

CASHIER
??? Ve y gestiona solo SU operator/brand y jugadores asignados
??? operatorId/brandId se resuelve automáticamente del contexto
??? Puede crear: más CASHIER, PLAYERS (se auto-asignan)
??? Puede ajustar wallets de jugadores asignados
??? Scope automático: su operator/brand + solo sus jugadores
```

## ?? Implementación

### 1. AuthorizationHelper (`apps/api/Casino.Api/Utils/AuthorizationHelper.cs`)

Clase helper que encapsula toda la lógica de autorización:

```csharp
// Resolver scope automáticamente
var (operatorScope, brandScope) = AuthorizationHelper.GetEffectiveScope(currentRole, brandContext);

// Para SUPER_ADMIN: (null, null) = sin restricciones
// Para otros roles: (brandContext.OperatorId, brandContext.BrandId) = scope automático
```

### 2. Validación de Permisos

```csharp
// Validar que el usuario tenga permisos para la operación
var permissionError = AuthorizationHelper.ValidateOperationPermissions(
    currentRole, 
    brandContext,
    requiredRoles: new[] { BackofficeUserRole.SUPER_ADMIN, BackofficeUserRole.OPERATOR_ADMIN },
    requireBrandContext: true);

if (permissionError != null)
    return permissionError; // Retorna 403 o 400
```

### 3. BrandId Efectivo para Creación

```csharp
// Para operaciones que crean entidades con brandId
var effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
    currentRole, 
    brandContext, 
    request.BrandId); // Solo SUPER_ADMIN puede especificar diferente

// Para otros roles: siempre usa brandContext.BrandId (transparente)
```

## ?? Cómo Aplicar a Otros Endpoints

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
    
    // Resolver operatorId automáticamente
    var effectiveOperatorId = currentRole == BackofficeUserRole.SUPER_ADMIN 
        ? request.OperatorId // SUPER_ADMIN puede especificar cualquiera
        : brandContext.OperatorId; // Otros usan el del contexto
    
    var effectiveRequest = request with { OperatorId = effectiveOperatorId };
    
    var response = await userService.CreateUserAsync(effectiveRequest, currentUserId);
    
    // ... resto de la lógica
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
    
    // El servicio usa operatorScope para filtrar automáticamente
    var response = await userService.GetUsersAsync(operatorScope);
    
    return TypedResults.Ok(response);
}
```

## ?? Transparencia del OperatorId

### ? ANTES (Manual)
```json
// El usuario tenía que especificar operatorId
POST /api/v1/admin/players
{
  "username": "player1",
  "operatorId": "11111111-1111-1111-1111-111111111111", // ? Manual
  "brandId": "22222222-2222-2222-2222-222222222222"
}
```

### ? AHORA (Transparente)
```json
// El operatorId se resuelve automáticamente del brand context
POST /api/v1/admin/players
{
  "username": "player1"
  // brandId se resuelve automáticamente del contexto (excepto SUPER_ADMIN)
  // operatorId se resuelve automáticamente del brandContext.OperatorId
}
```

## ?? Flujo de Resolución

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
   // operatorScope = "11111111-1111-1111-1111-111111111111" (automático)
   // brandScope = "22222222-2222-2222-2222-222222222222" (automático)
   ```

4. **Servicio** ? Filtra por scope automáticamente
   ```csharp
   query = query.Where(p => p.Brand.OperatorId == operatorScope);
   ```

## ? Beneficios

1. **Transparencia**: Usuarios no saben del operatorId, es automático
2. **Seguridad**: Imposible acceder a datos de otros operators
3. **Simplicidad**: Menos parámetros en requests
4. **Escalabilidad**: Fácil agregar nuevos brands/operators
5. **Mantenibilidad**: Lógica centralizada en AuthorizationHelper

## ?? Endpoints que Necesitan Actualización

- [ ] `BackofficeUserEndpoints.cs`
- [ ] `OperatorEndpoints.cs`
- [ ] `BrandGameEndpoints.cs`
- [ ] `CashierPlayerEndpoints.cs`
- [x] `PlayerManagementEndpoints.cs` ?
- [ ] `AdminEndpoints.cs`

Usar el patrón del `AuthorizationHelper` en todos estos endpoints para consistencia.