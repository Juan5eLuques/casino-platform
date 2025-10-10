# ? COMPLETADO: Transparencia Total de BrandId/OperatorId

## ?? Objetivo Logrado

Todos los endpoints ahora resuelven automáticamente `brandId` y `operatorId` desde el contexto de la URL/dominio, siendo **transparente** para los usuarios excepto `SUPER_ADMIN`.

## ?? Endpoints Actualizados

### ? **PlayerManagementEndpoints** 
- **DTO**: `CreatePlayerRequest.BrandId` es opcional
- **Resolución**: Automática desde `brandContext.BrandId`
- **Patrón**: `AuthorizationHelper.GetEffectiveScope()`

### ? **BackofficeUserEndpoints**
- **DTO**: `CreateBackofficeUserRequest.OperatorId` es opcional  
- **Resolución**: Automática desde `brandContext.OperatorId`
- **Patrón**: `AuthorizationHelper.GetEffectiveScope()`

### ? **BrandGameEndpoints**
- **Resolución**: Automática desde `brandContext.OperatorId`
- **Patrón**: `AuthorizationHelper.GetEffectiveScope()`

### ? **CashierPlayerEndpoints**
- **Resolución**: Automática desde `brandContext.OperatorId`
- **Patrón**: `AuthorizationHelper.GetEffectiveScope()`

## ?? Middlewares Corregidos

### ? **BrandResolverMiddleware**
```csharp
// ANTES: Saltaba resolución para /api/v1/admin/users
if (path.StartsWith("/api/v1/admin/users")) {
    await _next(context);
    return;
}

// AHORA: Solo salta para auth, health, swagger, gateway
if (path.StartsWith("/api/v1/admin/auth") || 
    path.StartsWith("/health") || ...) {
    await _next(context);
    return;
}
```

### ? **DynamicCorsMiddleware**
- Maneja automáticamente CORS por brand
- Nunca usa wildcard `*` con credentials
- Funciona con el brand context resuelto

## ?? DTOs Actualizados

### ? **CreatePlayerRequest**
```csharp
public record CreatePlayerRequest(
    string Username,
    string? Email = null,
    // ...
    Guid? BrandId = null); // Opcional - se resuelve automáticamente
```

### ? **CreateBackofficeUserRequest**
```csharp
public record CreateBackofficeUserRequest(
    string Username,
    string Password,
    BackofficeUserRole Role,
    Guid? OperatorId = null, // Opcional - se resuelve automáticamente
    // ...
);
```

## ?? Cómo Funciona Ahora

### **Request Típico (OPERATOR_ADMIN/CASHIER)**:
```json
POST https://admin.bet30.local:7182/api/v1/admin/users
{
  "username": "nuevo_cajero",
  "password": "password123",
  "role": "CASHIER",
  "commissionRate": 5.5
}
```

### **Flujo Interno**:
```
1. BrandResolverMiddleware
   ??? Resuelve "bet30" por dominio: admin.bet30.local
   ??? brandContext.BrandId = "22222222-..."
   ??? brandContext.OperatorId = "11111111-..."

2. BackofficeUserEndpoints
   ??? AuthorizationHelper.GetCurrentUserRole() = CASHIER
   ??? effectiveOperatorId = brandContext.OperatorId (automático)
   ??? request.OperatorId = null ? se fuerza al del contexto

3. BackofficeUserService
   ??? Valida operatorId efectivo (resuelto automáticamente)
   ??? Crea usuario con operatorId correcto
```

### **Request SUPER_ADMIN**:
```json
POST https://admin.bet30.local:7182/api/v1/admin/users
{
  "username": "admin_externo",
  "password": "password123",
  "role": "OPERATOR_ADMIN",
  "operatorId": "33333333-3333-3333-3333-333333333333"
}
```

## ?? Swagger Actualizado

### **Antes (Swagger mostraba campos requeridos)**:
```json
{
  "username": "string",
  "password": "string", 
  "role": "CASHIER",
  "operatorId": "uuid", // ? Campo visible en Swagger
  "brandId": "uuid"     // ? Campo visible en Swagger
}
```

### **Ahora (Campos opcionales en Swagger)**:
```json
{
  "username": "string",
  "password": "string",
  "role": "CASHIER",
  "commissionRate": 0
  // operatorId no aparece como requerido
  // brandId no aparece como requerido
}
```

## ? Validaciones por Rol

### **SUPER_ADMIN**:
- ? Puede especificar `operatorId`/`brandId` explícitamente
- ? Ve datos de todos los operators/brands
- ? Sin restricciones automáticas

### **OPERATOR_ADMIN**:
- ? `operatorId` se resuelve automáticamente del brand context
- ? Solo ve/gestiona su operator y brands asociados
- ? Puede crear: `OPERATOR_ADMIN`, `CASHIER`, `PLAYER`

### **CASHIER**:
- ? `operatorId` se resuelve automáticamente del brand context
- ? Solo ve/gestiona su operator/brand y jugadores asignados
- ? Puede crear: `CASHIER` (subordinados), `PLAYER` (auto-asignados)

## ?? AuthorizationHelper (Patrón Centralizado)

```csharp
// Todos los endpoints usan este patrón:
var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
var (operatorScope, brandScope) = AuthorizationHelper.GetEffectiveScope(currentRole, brandContext);

// Para creación/actualización:
var effectiveOperatorId = currentRole == BackofficeUserRole.SUPER_ADMIN 
    ? request.OperatorId 
    : brandContext.OperatorId;
```

## ?? Error 400 Resuelto

### **Problema Original**:
```
400 Bad Request - Brand Not Resolved
```

### **Causa**:
1. `BrandResolverMiddleware` saltaba `/api/v1/admin/users`
2. `brandContext.OperatorId` era `Guid.Empty`
3. Validaciones fallaban por falta de `operatorId`

### **Solución**:
1. ? Brand context se resuelve para todos los endpoints admin
2. ? `operatorId` se obtiene automáticamente de `brandContext.OperatorId`
3. ? Validaciones pasan y usuarios se crean exitosamente

## ?? Resultado Final

### **Frontend Benefits**:
- ? **Requests simplificados**: No más campos `operatorId`/`brandId` manuales
- ? **UX mejorada**: Usuarios no necesitan saber sobre operators
- ? **Formularios más limpios**: Swagger no muestra campos internos
- ? **Seguridad automática**: Imposible acceder a otros operators

### **Backend Benefits**:
- ? **Código consistente**: Mismo patrón en todos los endpoints
- ? **Mantenibilidad**: Lógica centralizada en `AuthorizationHelper`
- ? **Escalabilidad**: Fácil agregar nuevos brands/operators
- ? **Auditoría completa**: Logs estructurados con contexto

### **Operaciones Transparentes**:
```
? Crear jugadores       ? brandId automático
? Crear usuarios        ? operatorId automático  
? Asignar juegos        ? operatorId automático
? Gestión de cajeros    ? operatorId automático
? Consultas/listados    ? scope automático
? Ajustes de wallet     ? scope automático
```

## ?? Status: COMPLETADO ?

**Todos los endpoints admin ahora funcionan con transparencia total de `brandId`/`operatorId`:**

1. ? **PlayerManagementEndpoints** - Jugadores
2. ? **BackofficeUserEndpoints** - Usuarios backoffice  
3. ? **BrandGameEndpoints** - Gestión de juegos por brand
4. ? **CashierPlayerEndpoints** - Gestión de cajeros
5. ? **Middlewares** - Resolución y CORS automáticos
6. ? **DTOs** - Campos opcionales configurados
7. ? **AuthorizationHelper** - Patrón centralizado implementado

El sistema es ahora **verdaderamente multi-tenant** con resolución automática por URL y transparencia total para usuarios finales.